using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using System.Linq;
    using System.Net;
    using System.Runtime.CompilerServices;

    using Akka.Actor;
    using Akka.Event;

    using DI.P2P.Messages;

    public class PeerInfo
    {
        public Peer Peer { get; set; }

        public int ConnectionTries { get; set; }

        public IActorRef ProtocolHandler { get; set; }
    }

    public class BanInfo
    {
        public Peer Peer { get; set; }

        public DateTime BannedUntil { get; set; }
    }

    public class PeerRegistry : ReceiveActor
    {
        private readonly Peer selfPeer;

        public class AddPeer
        {
            public AddPeer(Peer peer, bool isConnected)
            {
                this.Peer = peer;
                this.IsConnected = isConnected;
            }

            public Peer Peer { get; }

            public bool IsConnected { get; }
        }

        public class BanPeer
        {
            public Peer Peer { get; }

            public DateTime BannedUntil { get; }

            public BanPeer(Peer peer, DateTime bannedUntil)
            {
                this.Peer = peer;
                this.BannedUntil = bannedUntil;
            }
        }

        public class AddPeers
        {
            public AddPeers(Peer[] peers)
            {
                this.Peers = peers;
            }

            public Peer[] Peers { get; }
        }

        public class GetPeers { }

        public class GetPeersResponse
        {
            public GetPeersResponse(PeerInfo[] peers)
            {
                this.Peers = peers;
            }

            public PeerInfo[] Peers { get; }
        }

        public class GetConnectedPeers { }

        public class GetConnectedPeersResponse
        {
            public GetConnectedPeersResponse(PeerInfo[] peers)
            {
                this.Peers = peers;
            }

            public PeerInfo[] Peers { get; }
        }

        public class GetBannedPeers { }

        public class GetBannedPeersResponse
        {
            public GetBannedPeersResponse(BanInfo[] peers)
            {
                this.Peers = peers;
            }

            public BanInfo[] Peers { get; }
        }

        public class PeerDisconnected
        {
            public PeerDisconnected(Peer peer)
            {
                this.Peer = peer;
            }

            public Peer Peer { get; }
        }

        public class TryConnection
        {
            public Peer Peer { get; }

            public TryConnection(Peer peer)
            {
                this.Peer = peer;
            }
        }

        public class DoHouseKeeping { }

        public class FindPeerInfo
        {
            public Peer Peer { get; }

            public FindPeerInfo(Peer peer)
            {
                this.Peer = peer;
            }
        }


        private readonly ILoggingAdapter log = Context.GetLogger();

        private readonly List<PeerInfo> peers = new List<PeerInfo>(100);

        private readonly List<PeerInfo> connectedPeers = new List<PeerInfo>(15);

        private readonly List<BanInfo> bannedPeers = new List<BanInfo>(15);

        public PeerRegistry()
        {
            var getSelfResponse = Context.ActorSelection("/user/Configuration")
                .Ask<Configuration.GetSelfResponse>(new Configuration.GetSelf()).Result;
            this.selfPeer = getSelfResponse.Self;

            this.Receive<AddPeer>(addPeer => this.ProcessAddPeer(addPeer));

            this.Receive<BanPeer>(banPeer => this.ProcessBanPeer(banPeer));

            this.Receive<AddPeers>(addPeers => this.ProcessAddPeers(addPeers.Peers));

            this.Receive<GetPeers>(_ => this.ProcessGetPeers());

            this.Receive<GetConnectedPeers>(_ => this.ProcessGetConnectedPeers());

            this.Receive<GetBannedPeers>(_ => this.ProcessGetBannedPeers());

            this.Receive<PeerDisconnected>(peerDisconnected => this.ProcessPeerDisconnected(peerDisconnected));

            this.Receive<TryConnection>(tryConnection => this.ProcessTryConnection(tryConnection));

            this.Receive<DoHouseKeeping>(_ => this.ProcessDoHouseKeeping());

            this.Receive<FindPeerInfo>(findPeerInfo => this.ProcessFindPeerInfo(findPeerInfo));

            Context.System.Scheduler
                .ScheduleTellRepeatedly(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), this.Self, new DoHouseKeeping(), ActorRefs.NoSender);

            var loadPeersReponse = Context.ActorSelection("/user/Configuration")
                .Ask<Configuration.LoadPeersReponse>(new Configuration.LoadPeers()).Result;

            foreach (var peer in loadPeersReponse.Peers)
            {
                this.Add(peer);
            }
        }

        private void Add(Peer peer, IActorRef protocolHandler = null)
        {
            // Do not add ourselfs to the registry.
            if (peer.Equals(this.selfPeer)) return;

            // Find the peer by id, or if that fails, find it by ip and port.
            var oldPeer = this.peers.FirstOrDefault(p => p.Peer.Equals(peer));

            // If the information we already have is newer, keep it.
            if (oldPeer != null && oldPeer.Peer.AnnounceTime > peer.AnnounceTime) return;

            if (oldPeer != null)
            {
                this.peers.Remove(oldPeer);

                if (oldPeer.Peer.Id != peer.Id)
                {
                    this.log.Debug($"Peer changed from {oldPeer.Peer} to {peer}");
                }
            }

            if (this.bannedPeers.Any(p => p.Peer.Equals(peer)))
            {
                // Do not add banned peers to the registry.
                return;
            }

            this.peers.Add(new PeerInfo { Peer = peer, ConnectionTries = oldPeer?.ConnectionTries ?? 0 });


            // Update the connected peers list.
            if (protocolHandler == null) return;

            var oldConnectedPeer = this.connectedPeers.FirstOrDefault(p => p.Peer.Equals(peer));

            if (oldConnectedPeer != null)
            {
                this.connectedPeers.Remove(oldConnectedPeer);
            }

            this.connectedPeers.Add(new PeerInfo { Peer = peer, ProtocolHandler = protocolHandler });
        }

        private void SavePeers()
        {
            Context.ActorSelection("/user/Configuration")
                .Tell(new Configuration.SavePeers(this.peers.Select(pi => pi.Peer).ToArray()));
        }

        private void ProcessAddPeer(AddPeer addPeer)
        {
            this.Add(addPeer.Peer, this.Sender);

            // Keep latest announced peers at the front of the list.
            this.peers.Sort((a, b) => a.Peer.AnnounceTime > b.Peer.AnnounceTime ? 1 : -1);

            this.log.Debug($"Add peer {addPeer.Peer} to the registry");

            this.SavePeers();
        }

        private void ProcessBanPeer(BanPeer banPeer)
        {
            var peerInfo = this.peers.FirstOrDefault(p => p.Peer.Equals(banPeer.Peer));

            if (peerInfo != null)
            {
                this.peers.Remove(peerInfo);
            }

            this.bannedPeers.Add(new BanInfo { Peer = banPeer.Peer, BannedUntil = banPeer.BannedUntil });

            this.log.Debug($"Banned peer {banPeer.Peer} until {banPeer.BannedUntil}");
        }

        private void ProcessAddPeers(Peer[] newPeers)
        {
            var nonConnectedPeers = newPeers.Where(p => this.connectedPeers.All(cp => !cp.Peer.Equals(p)));

            foreach (var peer in nonConnectedPeers)
            {
                this.Add(peer);
            }

            // Keep latest announced peers at the front of the list.
            this.peers.Sort((a, b) => a.Peer.AnnounceTime > b.Peer.AnnounceTime ? 1 : -1);

            this.log.Debug($"Added {newPeers.Length} peers to the registry");

            this.SavePeers();
        }

        private void ProcessGetPeers()
        {
            this.Sender.Tell(new GetPeersResponse(this.peers.Take(100).ToArray()));
        }

        private void ProcessGetConnectedPeers()
        {
            this.Sender.Tell(new GetConnectedPeersResponse(this.connectedPeers.ToArray()));
        }

        private void ProcessGetBannedPeers()
        {
            this.Sender.Tell(new GetBannedPeersResponse(this.bannedPeers.ToArray()));
        }

        private void ProcessPeerDisconnected(PeerDisconnected peerDisconnected)
        {
            var peer = this.connectedPeers.FirstOrDefault(cp => cp.Peer.Equals(peerDisconnected.Peer));

            if (peer == null) return;

            this.log.Debug($"Peer {peer} disconnected.");

            this.connectedPeers.Remove(peer);
        }

        private void ProcessTryConnection(TryConnection tryConnection)
        {
            var peerInfo = this.peers.FirstOrDefault(p => p.Peer.Equals(tryConnection.Peer));

            if (peerInfo == null) return;

            peerInfo.ConnectionTries++;
        }

        private void ProcessDoHouseKeeping()
        {
            var count = this.bannedPeers.RemoveAll(p => p.BannedUntil < DateTime.UtcNow);

            if (count > 0)
            {
                this.log.Debug($"Cleaned up {count} bans.");
            }
        }

        private void ProcessFindPeerInfo(FindPeerInfo findPeerInfo)
        {
            var peerInfo = this.connectedPeers.FirstOrDefault(p => p.Peer.Equals(findPeerInfo.Peer));
            this.Sender.Tell(peerInfo);
        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new PeerRegistry());
        }
    }
}

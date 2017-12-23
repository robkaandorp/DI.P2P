using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using System.Linq;
    using System.Net;

    using Akka.Actor;
    using Akka.Event;

    using DI.P2P.Messages;

    public class PeerInfo
    {
        public Peer Peer { get; set; }

        public int ConnectionTries { get; set; }
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


        private readonly ILoggingAdapter log = Context.GetLogger();

        private readonly List<PeerInfo> peers = new List<PeerInfo>(100);

        private readonly List<PeerInfo> connectedPeers = new List<PeerInfo>(15);

        public PeerRegistry(Peer selfPeer)
        {
            this.selfPeer = selfPeer;

            this.Receive<AddPeer>(addPeer => this.ProcessAddPeer(addPeer));

            this.Receive<AddPeers>(addPeers => this.ProcessAddPeers(addPeers.Peers));

            this.Receive<GetPeers>(_ => this.ProcessGetPeers());

            this.Receive<GetConnectedPeers>(_ => this.ProcessGetConnectedPeers());

            this.Receive<PeerDisconnected>(peerDisconnected => this.ProcessPeerDisconnected(peerDisconnected));

            this.Receive<TryConnection>(tryConnection => this.ProcessTryConnection(tryConnection));
        }

        private void Add(Peer peer, bool isConnected)
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

            this.peers.Add(new PeerInfo { Peer = peer, ConnectionTries = oldPeer?.ConnectionTries ?? 0 });


            // Update the connected peers list.
            if (!isConnected) return;

            var oldConnectedPeer = this.connectedPeers.FirstOrDefault(p => p.Peer.Equals(peer));

            if (oldConnectedPeer != null)
            {
                this.connectedPeers.Remove(oldConnectedPeer);
            }

            this.connectedPeers.Add(new PeerInfo { Peer = peer });
        }

        private void ProcessAddPeer(AddPeer addPeer)
        {
            this.Add(addPeer.Peer, addPeer.IsConnected);

            // Keep latest announced peers at the front of the list.
            this.peers.Sort((a, b) => a.Peer.AnnounceTime > b.Peer.AnnounceTime ? 1 : -1);

            this.log.Debug($"Add peer {addPeer.Peer} to the registry");
        }

        private void ProcessAddPeers(Peer[] newPeers)
        {
            var nonConnectedPeers = newPeers.Where(p => this.connectedPeers.All(cp => !cp.Peer.Equals(p)));

            foreach (var peer in nonConnectedPeers)
            {
                this.Add(peer, false);
            }

            // Keep latest announced peers at the front of the list.
            this.peers.Sort((a, b) => a.Peer.AnnounceTime > b.Peer.AnnounceTime ? 1 : -1);

            this.log.Debug($"Added {newPeers.Length} peers to the registry");
        }

        private void ProcessGetPeers()
        {
            this.Sender.Tell(new GetPeersResponse(this.peers.Take(100).ToArray()));
        }

        private void ProcessGetConnectedPeers()
        {
            this.Sender.Tell(new GetConnectedPeersResponse(this.connectedPeers.ToArray()));
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

        public static Props Props(Peer selfPeer)
        {
            return Akka.Actor.Props.Create(() => new PeerRegistry(selfPeer));
        }
    }
}

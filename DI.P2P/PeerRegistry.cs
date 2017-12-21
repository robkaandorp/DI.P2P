using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using System.Linq;

    using Akka.Actor;
    using Akka.Event;

    using DI.P2P.Messages;

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
            public GetPeersResponse(Peer[] peers)
            {
                this.Peers = peers;
            }

            public Peer[] Peers { get; }
        }

        public class GetConnectedPeers { }

        public class GetConnectedPeersResponse
        {
            public GetConnectedPeersResponse(Peer[] peers)
            {
                this.Peers = peers;
            }

            public Peer[] Peers { get; }
        }


        private readonly ILoggingAdapter log = Context.GetLogger();

        private readonly List<Peer> peers = new List<Peer>(100);

        private readonly List<Peer> connectedPeers = new List<Peer>(15);

        public PeerRegistry(Peer selfPeer)
        {
            this.selfPeer = selfPeer;

            this.Receive<AddPeer>(addPeer => this.ProcessAddPeer(addPeer));

            this.Receive<AddPeers>(addPeers => this.ProcessAddPeers(addPeers.Peers));

            this.Receive<GetPeers>(_ => this.ProcessGetPeers());

            this.Receive<GetConnectedPeers>(_ => this.ProcessGetConnectedPeers());
        }

        private void Add(Peer peer, bool isConnected)
        {
            // Do not add ourselfs to the registry.
            if (peer.Id == this.selfPeer.Id) return;

            var oldPeer = this.peers.FirstOrDefault(p => p.Id == peer.Id);

            // If the information we already have is newer, keep it.
            if (oldPeer != null && oldPeer.AnnounceTime > peer.AnnounceTime) return;

            if (oldPeer != null)
            {
                this.peers.Remove(oldPeer);
            }

            this.peers.Add(peer);


            // Update the connected peers list.
            if (!isConnected) return;

            var oldConnectedPeer = this.connectedPeers.FirstOrDefault(p => p.Id == peer.Id);

            if (oldConnectedPeer != null)
            {
                this.connectedPeers.Remove(oldConnectedPeer);
            }

            this.connectedPeers.Add(peer);
        }

        private void ProcessAddPeer(AddPeer addPeer)
        {
            this.Add(addPeer.Peer, addPeer.IsConnected);

            // Keep latest announced peers at the front of the list.
            this.peers.Sort((a, b) => a.AnnounceTime > b.AnnounceTime ? 1 : -1);

            this.log.Debug($"Add peer {addPeer.Peer} to the registry");
        }

        private void ProcessAddPeers(Peer[] newPeers)
        {
            var nonConnectedPeers = newPeers.Where(p => this.connectedPeers.All(cp => cp.Id != p.Id));

            foreach (var peer in nonConnectedPeers)
            {
                this.Add(peer, false);
            }

            // Keep latest announced peers at the front of the list.
            this.peers.Sort((a, b) => a.AnnounceTime > b.AnnounceTime ? 1 : -1);

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

        public static Props Props(Peer selfPeer)
        {
            return Akka.Actor.Props.Create(() => new PeerRegistry(selfPeer));
        }
    }
}

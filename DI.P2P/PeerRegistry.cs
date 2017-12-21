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
            public AddPeer(Peer peer)
            {
                this.Peer = peer;
            }

            public Peer Peer { get; }
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


        private readonly ILoggingAdapter log = Context.GetLogger();

        private readonly List<Peer> peers = new List<Peer>(100);

        public PeerRegistry(Peer selfPeer)
        {
            this.selfPeer = selfPeer;

            this.Receive<AddPeer>(addPeer => this.ProcessAddPeer(addPeer.Peer));

            this.Receive<AddPeers>(addPeers => this.ProcessAddPeers(addPeers.Peers));

            this.Receive<GetPeers>(_ => this.ProcessGetPeers());
        }

        private void ProcessAddPeer(Peer peer)
        {
            // Do not add ourselfs to the registry.
            if (peer.Id == this.selfPeer.Id) return;

            var oldPeer = this.peers.FirstOrDefault(p => p.Id == peer.Id);

            if (oldPeer != null)
            {
                this.peers.Remove(oldPeer);
            }

            this.peers.Add(peer);

            // Keep latest announced peers at the front of the list.
            this.peers.Sort((a, b) => a.AnnounceTime > b.AnnounceTime ? 1 : -1);

            this.log.Debug($"Add peer {peer} to the registry");
        }

        private void ProcessAddPeers(Peer[] newPeers)
        {
            foreach (var peer in newPeers)
            {
                // Do not add ourselfs to the registry.
                if (peer.Id == this.selfPeer.Id) continue;

                this.peers.Add(peer);
            }

            // Keep latest announced peers at the front of the list.
            this.peers.Sort((a, b) => a.AnnounceTime > b.AnnounceTime ? 1 : -1);

            this.log.Debug($"Added {newPeers.Length} peers to the registry");
        }

        private void ProcessGetPeers()
        {
            this.Sender.Tell(new GetPeersResponse(this.peers.Take(100).ToArray()));
        }

        public static Props Props(Peer selfPeer)
        {
            return Akka.Actor.Props.Create(() => new PeerRegistry(selfPeer));
        }
    }
}

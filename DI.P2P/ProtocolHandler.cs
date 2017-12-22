using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using System.Linq;

    using Akka.Actor;
    using Akka.Event;
    using Akka.Util.Internal;

    using DI.P2P.Messages;

    public class ProtocolHandler : ReceiveActor
    {
        public class Connected { }

        public class Disconnected { }


        private readonly Peer selfPeer;

        private Peer connectedPeer;

        private readonly bool isClient;

        private readonly ILoggingAdapter log = Context.GetLogger();

        public ProtocolHandler(Peer selfPeer, bool isClient)
        {
            this.selfPeer = selfPeer;
            this.isClient = isClient;


            this.Receive<Connected>(connected => this.ProcessConnected());

            this.Receive<Disconnected>(disconnected => this.ProcessDisconnected());

            this.Receive<AnnounceMessage>(msg => this.ProcessAnnounceMessage(msg));
        }

        private bool announceMessageSent = false;

        private void SendAnnounceMessage()
        {
            var response = Context.ActorSelection("/user/PeerRegistry")
                .Ask<PeerRegistry.GetPeersResponse>(new PeerRegistry.GetPeers()).Result;

            Context.ActorSelection("../MessageLayer").Tell(
                new AnnounceMessage
                    {
                        Peer = this.selfPeer,
                        Peers = response.Peers.Select(p => p.Peer).ToArray(),
                        MyTime = DateTime.UtcNow
                    });
            this.announceMessageSent = true;
        }

        private void ProcessAnnounceMessage(AnnounceMessage msg)
        {
            // Correct time difference in clocktime of both nodes.
            msg.Peer.AnnounceTime = DateTime.UtcNow;
            var timeOffset = msg.Peer.AnnounceTime - msg.MyTime;
            msg.Peers?.ForEach(p => p.AnnounceTime += timeOffset);

            this.log.Debug($"Announce received; {msg.Peer}, time offset {timeOffset}");

            this.connectedPeer = msg.Peer;

            if (!this.isClient)
            {
                // Send response with an announce message and a list of peers.
                this.SendAnnounceMessage();
            }

            if (string.IsNullOrWhiteSpace(msg.Peer.IpAddress))
            {
                var response = Context.Parent.Ask<TcpConnection.GetRemoteIpResponse>(new TcpConnection.GetRemoteIp()).Result;
                msg.Peer.IpAddress = response.IpAddress;
            }

            var peerRegistry = Context.ActorSelection("/user/PeerRegistry");
            peerRegistry.Tell(new PeerRegistry.AddPeer(msg.Peer, true));

            if (msg.Peers != null && msg.Peers.Length > 0)
            {
                peerRegistry.Tell(new PeerRegistry.AddPeers(msg.Peers));
            }
        }

        private void ProcessConnected()
        {
            if (!this.announceMessageSent && this.isClient)
            {
                this.SendAnnounceMessage();
            }
        }

        private void ProcessDisconnected()
        {
            if (this.connectedPeer == null) return;

            var peerRegistry = Context.ActorSelection("/user/PeerRegistry");
            peerRegistry.Tell(new PeerRegistry.PeerDisconnected(this.connectedPeer));
        }

        public static Props Props(Peer selfPeer, bool isClient)
        {
            return Akka.Actor.Props.Create(() => new ProtocolHandler(selfPeer, isClient));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using Akka.Actor;
    using Akka.Event;
    using Akka.Util.Internal;

    using DI.P2P.Messages;

    public class ProtocolHandler : ReceiveActor
    {
        public class ConnectMessageLayer
        {
            public IActorRef MessageLayer { get; }

            public ConnectMessageLayer(IActorRef messageLayer)
            {
                this.MessageLayer = messageLayer;
            }
        }


        private readonly Peer selfPeer;

        private readonly bool isClient;

        private readonly ILoggingAdapter log = Context.GetLogger();

        private IActorRef messageLayer;

        public ProtocolHandler(Peer selfPeer, bool isClient)
        {
            this.selfPeer = selfPeer;
            this.isClient = isClient;

            this.Receive<ConnectMessageLayer>(connectMessageLayer =>
                {
                    this.messageLayer = connectMessageLayer.MessageLayer;

                    if (!this.announceMessageSent && this.isClient)
                    {
                        this.SendAnnounceMessage();
                    }
                });

            this.Receive<AnnounceMessage>(msg => this.ProcessAnnounceMessage(msg));
        }

        private bool announceMessageSent = false;

        private void SendAnnounceMessage()
        {
            var response = Context.ActorSelection("/user/PeerPool/PeerRegistry")
                .Ask<PeerRegistry.GetPeersResponse>(new PeerRegistry.GetPeers()).Result;

            this.messageLayer.Tell(
                new AnnounceMessage
                    {
                        Peer = this.selfPeer,
                        SoftwareVersion = Versions.SoftwareVersion,
                        ProtocolVersion = Versions.ProtocolVersion,
                        Peers = response.Peers,
                        MyTime = DateTime.UtcNow
                    });
            this.announceMessageSent = true;
        }

        private void ProcessAnnounceMessage(AnnounceMessage msg)
        {
            this.log.Debug($"Announce received; {msg.Peer.Id} {msg.SoftwareVersion} - protocol {msg.ProtocolVersion}");
            msg.Peer.AnnounceTime = DateTime.UtcNow;

            if (!this.isClient)
            {
                // Send response with an announce message and a list of peers.
                this.SendAnnounceMessage();
            }

            // Correct time difference in clocktime of both nodes.
            var timeOffset = msg.Peer.AnnounceTime - msg.MyTime;
            msg.Peers?.ForEach(p => p.AnnounceTime += timeOffset);

            if (string.IsNullOrWhiteSpace(msg.Peer.IpAddress))
            {
                var response = Context.Parent.Ask<TcpConnection.GetRemoteIpResponse>(new TcpConnection.GetRemoteIp()).Result;
                msg.Peer.IpAddress = response.IpAddress;
            }

            var peerRegistry = Context.ActorSelection("/user/PeerPool/PeerRegistry");
            peerRegistry.Tell(new PeerRegistry.AddPeer(msg.Peer));

            if (msg.Peers != null && msg.Peers.Length > 0)
            {
                peerRegistry.Tell(new PeerRegistry.AddPeers(msg.Peers));
            }
        }

        public static Props Props(Peer selfPeer, bool isClient)
        {
            return Akka.Actor.Props.Create(() => new ProtocolHandler(selfPeer, isClient));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using System.Net;

    using Akka.Actor;
    using Akka.Event;
    using Akka.IO;

    using DI.P2P.Messages;

    public class PeerPool : ReceiveActor
    {
        private readonly Peer selfPeer;

        private IActorRef peerRegistry;

        public class ConnectTo
        {
            public Peer Peer { get; set; }
        }

        private readonly ILoggingAdapter log = Context.GetLogger();

        public PeerPool(Peer selfPeer)
        {
            this.selfPeer = selfPeer;

            this.peerRegistry = Context.ActorOf(PeerRegistry.Props(this.selfPeer), "PeerRegistry");

            this.Receive<ConnectTo>(connectTo => this.ProcessConnectTo(connectTo));

            this.Receive<Tcp.Connected>(connected => this.ProcessConnected(connected));

            this.Receive<Tcp.CommandFailed>(
                commandFailed =>
                    {
                        this.log.Error($"Connection to peer failed. {commandFailed}");
                        this.Self.GracefulStop(TimeSpan.FromSeconds(10));
                    });
        }

        private void ProcessConnectTo(ConnectTo connectTo)
        {
            var endpoint = new DnsEndPoint(connectTo.Peer.IpAddress, connectTo.Peer.Port);
            Context.System.Tcp().Tell(new Tcp.Connect(endpoint));
        }

        private void ProcessConnected(Tcp.Connected connected)
        {
            this.log.Debug($"Connected to peer; {connected}");
            var clientConnection = Context.ActorOf(TcpConnection.Props(this.Sender, this.selfPeer, ((IPEndPoint)connected.RemoteAddress).Address.ToString(), true));
            this.Sender.Tell(new Tcp.Register(clientConnection));
        }

        public static Props Props(Peer selfPeer)
        {
            return Akka.Actor.Props.Create(() => new PeerPool(selfPeer));
        }
    }
}

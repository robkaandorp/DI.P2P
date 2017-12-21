using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using System.Linq;
    using System.Net;

    using Akka.Actor;
    using Akka.Event;
    using Akka.IO;
    using Akka.Util.Internal;

    using DI.P2P.Messages;

    public class PeerPool : ReceiveActor
    {
        private readonly Peer selfPeer;

        public class ConnectTo
        {
            public Peer Peer { get; set; }
        }

        public class EnsureConnectionsMessage { }

        private readonly ILoggingAdapter log = Context.GetLogger();

        public PeerPool(Peer selfPeer)
        {
            this.selfPeer = selfPeer;

            this.Receive<ConnectTo>(connectTo => this.ProcessConnectTo(connectTo));

            this.Receive<Tcp.Connected>(connected => this.ProcessConnected(connected));

            this.Receive<Tcp.CommandFailed>(
                commandFailed =>
                    {
                        this.log.Error($"Connection to peer failed. {commandFailed}");
                        this.Self.GracefulStop(TimeSpan.FromSeconds(10));
                    });

            this.Receive<EnsureConnectionsMessage>(_ => this.EnsureConnections());

            Context.System.Scheduler
                .ScheduleTellRepeatedly(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), this.Self, new EnsureConnectionsMessage(), ActorRefs.NoSender);
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

        private void EnsureConnections()
        {
            var connectedPeersResponse = Context.System.ActorSelection("/user/PeerRegistry")
                .Ask<PeerRegistry.GetConnectedPeersResponse>(new PeerRegistry.GetConnectedPeers()).Result;

            if (connectedPeersResponse.Peers.Length >= 15) return; // We have enough connections.

            var peersResponse = Context.System.ActorSelection("/user/PeerRegistry")
                .Ask<PeerRegistry.GetPeersResponse>(new PeerRegistry.GetPeers()).Result;

            var unconnectedPeers = peersResponse.Peers
                .Where(p => connectedPeersResponse.Peers.All(cp => p.Id != cp.Id))
                .Take(2);

            unconnectedPeers.ForEach(
                p =>
                    {
                        var endpoint = new DnsEndPoint(p.IpAddress, p.Port);
                        Context.System.Tcp().Tell(new Tcp.Connect(endpoint));
                    });
        }

        public static Props Props(Peer selfPeer)
        {
            return Akka.Actor.Props.Create(() => new PeerPool(selfPeer));
        }
    }
}

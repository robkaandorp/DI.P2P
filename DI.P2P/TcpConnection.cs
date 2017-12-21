namespace DI.P2P
{
    using System;
    using System.Net;

    using Akka.Actor;
    using Akka.Event;
    using Akka.IO;

    using DI.P2P.Messages;

    /// <summary>
    /// Handles sending and receiving of data over the socket.
    /// </summary>
    public class TcpConnection : UntypedActor
    {
        public class GetRemoteIp { }

        public class GetRemoteIpResponse
        {
            public GetRemoteIpResponse(string ipAddress)
            {
                this.IpAddress = ipAddress;
            }

            public string IpAddress { get; }
        }


        private readonly ILoggingAdapter log = Context.GetLogger();

        private readonly IActorRef connection;

        private readonly Peer selfPeer;

        private readonly bool isClient;

        private readonly string remoteIpAddress;

        private IActorRef transportLayer;

        private byte transportLayerVersion = 0;

        public TcpConnection(IActorRef connection, Peer selfPeer, string remoteIpAddress, bool isClient)
        {
            this.connection = connection;
            this.selfPeer = selfPeer;
            this.isClient = isClient;
            this.remoteIpAddress = remoteIpAddress;

            if (!this.isClient)
            {
                // Send our transport layer version as the first byte.
                this.connection.Tell(Tcp.Write.Create(ByteString.FromBytes(new[] { Versions.TransportLayer })));
            }
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Tcp.Received _:
                    var received = (Tcp.Received)message;

                    if (received.Data.IsEmpty)
                    {
                        return;
                    }

                    var data = received.Data;

                    if (this.transportLayerVersion == 0)
                    {
                        this.log.Debug($"Remote transport layer version: {data[0]}, our version {Versions.TransportLayer}");

                        // Initialize transport layer version once, selecting the lowest of both sides.
                        this.transportLayerVersion = Math.Min(data[0], Versions.TransportLayer);
                        data = data.Slice(1);

                        if (this.isClient)
                        {
                            // Now tell the peer our version.
                            this.connection.Tell(Tcp.Write.Create(ByteString.FromBytes(new[] { this.transportLayerVersion })));
                        }

                        var protocolHandler = Context.ActorOf(ProtocolHandler.Props(this.selfPeer, this.isClient), "ProtocolHandler");
                        var messageLayer = Context.ActorOf(MessageLayer.Props(protocolHandler), "MessageLayer");
                        this.transportLayer = Context.ActorOf(TransportLayer.Props(this.Self, this.transportLayerVersion, messageLayer), "TransportLayer");
                    }

                    this.transportLayer.Tell(data);
                    break;

                case ByteString _:
                    this.connection.Tell(Tcp.Write.Create((ByteString)message));
                    break;

                case GetRemoteIp _:
                    this.Sender.Tell(new GetRemoteIpResponse(this.remoteIpAddress));
                    break;

                case Tcp.ConnectionClosed _:
                    this.log.Debug($"ConnectionClosed; Shutting down peer.. {message}");
                    this.Self.GracefulStop(TimeSpan.FromSeconds(10));
                    break;

                default:
                    this.Unhandled(message);
                    break;
            }
        }

        public static Props Props(IActorRef connection, Peer selfPeer, string remoteIpAddress, bool isClient = false)
        {
            return Akka.Actor.Props.Create(() => new TcpConnection(connection, selfPeer, remoteIpAddress, isClient));
        }
    }
}

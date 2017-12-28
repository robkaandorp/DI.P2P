using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using System.Net;

    using Akka.Actor;
    using Akka.Event;
    using Akka.IO;

    using DI.P2P.Connection;
    using DI.P2P.Messages;

    /// <summary>
    /// Handles incoming socket connections.
    /// </summary>
    public class TcpServer : UntypedActor
    {
        private readonly Peer selfPeer;

        private readonly ILoggingAdapter log = Context.GetLogger();

        public TcpServer(Peer selfPeer)
        {
            this.selfPeer = selfPeer;
            Context.System.Tcp().Tell(new Tcp.Bind(this.Self, new IPEndPoint(IPAddress.Any, this.selfPeer.Port)));
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Tcp.Bound _:
                    var bound = (Tcp.Bound)message;
                    this.log.Info("Listening on {0}", bound.LocalAddress);
                    break;
                case Tcp.Connected connected:
                    this.log.Debug("{0}", connected);
                    var connection = Context.ActorOf(TcpConnection.Props(this.Sender, this.selfPeer, ((IPEndPoint)connected.RemoteAddress).Address.ToString()));
                    this.Sender.Tell(new Tcp.Register(connection));
                    break;
                default:
                    this.Unhandled(message);
                    break;
            }
        }

        public static Props Props(Peer selfPeer)
        {
            return Akka.Actor.Props.Create(() => new TcpServer(selfPeer));
        }
    }
}

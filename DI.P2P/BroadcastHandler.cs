using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using System.Linq;
    using System.Threading.Tasks;

    using Akka.Actor;

    using DI.P2P.Connection;
    using DI.P2P.Messages;

    public class BroadcastHandler : ReceiveActor
    {
        public class SendBroadcast
        {
            public byte[] Data { get; }

            public SendBroadcast(byte[] data)
            {
                this.Data = data;
            }
        }

        public class BroadcastReceived
        {
            public BroadcastReceived(BroadcastMessage message, Peer @on)
            {
                this.Message = message;
                this.On = @on;
            }

            public BroadcastMessage Message { get; }

            public Peer On { get; }
        }

        public class RegisterHandler
        {
            public RegisterHandler(Action<BroadcastMessage> handler)
            {
                this.Handler = handler;
            }

            public Action<BroadcastMessage> Handler { get; }
        }


        private readonly List<Action<BroadcastMessage>> handlers = new List<Action<BroadcastMessage>>();

        private readonly Dictionary<Guid, DateTime> receivedBroadcasts = new Dictionary<Guid, DateTime>(1000);

        private readonly Peer selfPeer;

        public BroadcastHandler()
        {
            var getSelfResponse = Context.ActorSelection("/user/Configuration")
                .Ask<Configuration.GetSelfResponse>(new Configuration.GetSelf()).Result;
            this.selfPeer = getSelfResponse.Self;

            this.Receive<SendBroadcast>(sendBroadcast => this.ProcessSendBroadcast(sendBroadcast));

            this.Receive<BroadcastReceived>(broadcastReceived => this.ProcessBroadcastReceived(broadcastReceived));

            this.Receive<RegisterHandler>(registerHandler => this.handlers.Add(registerHandler.Handler));

            // TODO implement houdkeeping to purge receivedBroadcasts
        }

        private void ProcessSendBroadcast(SendBroadcast sendBroadcast)
        {
            var message = new BroadcastMessage
                {
                    Data = sendBroadcast.Data,
                    From = this.selfPeer.Id,
                    Id = Guid.NewGuid()
                };

            var response = Context.ActorSelection("/user/PeerRegistry")
                .Ask<PeerRegistry.GetConnectedPeersResponse>(new PeerRegistry.GetConnectedPeers()).Result;

            foreach (var peer in response.Peers)
            {
                peer.ProtocolHandler.Tell(new ProtocolHandler.ForwardBroadcast(message));
            }
        }

        private void ProcessBroadcastReceived(BroadcastReceived broadcastReceived)
        {
            // Stop processing if we already received the broadcast.
            if (this.receivedBroadcasts.ContainsKey(broadcastReceived.Message.Id)) return;

            this.receivedBroadcasts.Add(broadcastReceived.Message.Id, DateTime.UtcNow);

            // Relay to all connection except the one we received the message on.
            var connectedPeersResponse = Context.ActorSelection("/user/PeerRegistry")
                .Ask<PeerRegistry.GetConnectedPeersResponse>(new PeerRegistry.GetConnectedPeers()).Result;

            var broadcastTo = connectedPeersResponse.Peers.Where(
                pi => pi.Peer.Id != broadcastReceived.On.Id && pi.Peer.Id != broadcastReceived.Message.From);

            foreach (var peer in broadcastTo)
            {
                peer.ProtocolHandler.Tell(new ProtocolHandler.ForwardBroadcast(broadcastReceived.Message));
            }

            this.handlers.ForEach(handler => Task.Run(() => handler(broadcastReceived.Message)));
        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new BroadcastHandler());
        }
    }
}

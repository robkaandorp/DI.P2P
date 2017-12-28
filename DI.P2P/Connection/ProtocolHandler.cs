namespace DI.P2P.Connection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    using Akka.Actor;
    using Akka.Event;
    using Akka.Util.Internal;

    using DI.P2P.Messages;

    public class ProtocolHandler : ReceiveActor
    {
        public class Connected { }

        public class Disconnected { }

        public class SendPing { }

        public class ReceivePong
        {
            public string ErrorMsg { get; }

            public ReceivePong(string errorMsg)
            {
                this.ErrorMsg = errorMsg;
            }
        }


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

            this.Receive<KeyExchangeMessage>(keyExchangeMessage => this.ProcessKeyExchangeMessage(keyExchangeMessage));

            this.Receive<DisconnectAndRemove>(msg => this.ProcessDisconnectAndRemove(msg));

            this.Receive<BroadcastMessage>(broadcastMessage => this.ProcessBroadcastMessage(broadcastMessage));

            this.Receive<Ping>(ping => this.ProcessPing(ping));

            this.Receive<Pong>(pong => this.ProcessPong(pong));

            this.Receive<SendPing>(sendPing => this.ProcessSendPing(sendPing));

            //Context.System.Scheduler.ScheduleTellRepeatedly(
            //    TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), Context.ActorSelection("../MessageLayer"), new Ping(), this.Self);
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
            this.SendKeyExchangeMessage();

            if (!this.isClient)
            {
                // Make sure we did not connect to ourselfs.
                if (msg.Peer.Equals(this.selfPeer))
                {
                    this.log.Debug("Connected to ourselfs, requesting disconnect.");
                    Context.ActorSelection("../MessageLayer")
                        .Tell(new DisconnectAndRemove { Reason = "Self connection", Peer = this.selfPeer });
                    return;
                }

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

        private void ProcessDisconnectAndRemove(DisconnectAndRemove msg)
        {
            this.log.Debug($"DisconnectAndRemove received; reason '{msg.Reason}'");

            msg.Peer.AnnounceTime = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(msg.Peer.IpAddress))
            {
                var response = Context.Parent.Ask<TcpConnection.GetRemoteIpResponse>(new TcpConnection.GetRemoteIp()).Result;
                msg.Peer.IpAddress = response.IpAddress;
            }

            Context.ActorSelection("/user/PeerRegistry")
                .Tell(new PeerRegistry.BanPeer(msg.Peer, DateTime.UtcNow.AddDays(1)));

            Context.Parent.Tell(new TcpConnection.Disconnect());
        }

        private void SendKeyExchangeMessage()
        {
            var msg = new KeyExchangeMessage();

            using (var aes = Aes.Create())
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(
                        new RSAParameters
                            {
                                Modulus = this.connectedPeer.RsaParameters.Modulus,
                                Exponent = this.connectedPeer.RsaParameters.Exponent
                            });

                    Context.ActorSelection("../TransportLayer").Tell(new TransportLayer.SetAesKeyIn(aes.Key));

                    msg.Key = rsa.Encrypt(aes.Key, true);
                }
            }

            Context.ActorSelection("../MessageLayer").Tell(msg);
        }

        private void ProcessKeyExchangeMessage(KeyExchangeMessage keyExchangeMessage)
        {
            var key = keyExchangeMessage.Key;

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(this.selfPeer.InternalRsaParameters);
                key = rsa.Decrypt(key, true);
            }

            Context.ActorSelection("../TransportLayer").Tell(new TransportLayer.SetAesKeyOut(key));
        }

        private void ProcessBroadcastMessage(BroadcastMessage broadcastMessage)
        {
            // Immidiately send the message to all connected peers.
            Context.ActorSelection("/*/MessageLayer").Tell(broadcastMessage);

            var data = Encoding.UTF8.GetString(broadcastMessage.Data);
            this.log.Info($"Received broadcast {broadcastMessage.Id} from {broadcastMessage.From}; '{data}'");
        }

        private void ProcessPing(Ping ping)
        {
            this.log.Debug($"Received ping from {this.connectedPeer}, returning pong.");
            this.Sender.Tell(new Pong(ping.Data, ping.TellPath));
        }

        private void ProcessPong(Pong pong)
        {
            this.log.Debug($"Received pong from {this.connectedPeer}.");

            string errorMsg = string.Empty;

            if (pong.Data[0] != 0 || pong.Data[1] != 1 || pong.Data[2] != 2 || pong.Data[3] != 3)
            {
                errorMsg = "Ping data garbled";
            }

            if (!string.IsNullOrWhiteSpace(pong.TellPath))
            {
                if (this.pingQueue.TryDequeue(out var tellTo))
                {
                    if (!tellTo.Path.ToString().Equals(pong.TellPath))
                    {
                        errorMsg += "Responding to incorrect actor.";
                    }

                    tellTo.Tell(new ReceivePong(errorMsg));
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    this.log.Error(errorMsg);
                }
            }
        }

        private readonly Queue<IActorRef> pingQueue = new Queue<IActorRef>();

        private void ProcessSendPing(SendPing sendPing)
        {
            this.pingQueue.Enqueue(this.Sender);
            Context.ActorSelection("../MessageLayer").Tell(new Ping(new byte[] { 0, 1, 2, 3 }, this.Sender.Path.ToString()));
        }

        public static Props Props(Peer selfPeer, bool isClient)
        {
            return Akka.Actor.Props.Create(() => new ProtocolHandler(selfPeer, isClient));
        }
    }
}

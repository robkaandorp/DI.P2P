namespace DI.P2P
{
    using System;
    using System.Threading.Tasks;

    using Akka.Actor;
    using Akka.Configuration;
    using Akka.Configuration.Hocon;

    using DI.P2P.Messages;

    public class P2PSystem : IComponent
    {
        private readonly Peer selfPeer;

        private ActorSystem system;

        private IActorRef tcpServer;

        private IActorRef peerPool;

        private IActorRef peerRegistry;

        public P2PSystem(Module owner, Peer selfPeer)
        {
            this.selfPeer = selfPeer;
            this.Owner = owner;
        }

        public async Task Start()
        {
            if (this.IsRunning)
            {
                return;
            }

            var config = ConfigurationFactory.ParseString(
                @"akka {  
                    stdout-loglevel = DEBUG
                    loglevel = DEBUG
                }");

            this.system = ActorSystem.Create("P2PSystem", config);
            this.tcpServer = this.system.ActorOf(TcpServer.Props(this.selfPeer), "TcpServer");
            this.peerPool = this.system.ActorOf(PeerPool.Props(this.selfPeer), "PeerPool");
            this.peerRegistry = this.system.ActorOf(PeerRegistry.Props(this.selfPeer), "PeerRegistry");

            this.IsRunning = true;
        }

        public async Task Stop()
        {
            if (!this.IsRunning)
            {
                return;
            }

            await this.system.Terminate();
        }

        public bool IsRunning { get; private set; }

        public Module Owner { get; }

        public void AddNode(string node)
        {
            var parts = node.Split(':');

            var ipAddress = parts[0];
            int port = 11111;

            if (parts.Length > 1)
            {
                if (!int.TryParse(parts[1], out port))
                {
                    throw new Exception($"Invalid port number {parts[1]}");
                }
            }

            this.peerPool.Tell(new PeerPool.ConnectTo { Peer = new Peer { IpAddress = ipAddress, Port = port } });
        }

        public PeerInfo[] GetConnectedPeers()
        {
            var response = this.peerRegistry.Ask<PeerRegistry.GetConnectedPeersResponse>(new PeerRegistry.GetConnectedPeers()).Result;
            return response.Peers;
        }

        public PeerInfo[] GetPeers()
        {
            var response = this.peerRegistry.Ask<PeerRegistry.GetPeersResponse>(new PeerRegistry.GetPeers()).Result;
            return response.Peers;
        }

        public PeerInfo[] GetBannedPeers()
        {
            var response = this.peerRegistry.Ask<PeerRegistry.GetBannedPeersResponse>(new PeerRegistry.GetBannedPeers()).Result;
            return response.Peers;
        }
    }
}

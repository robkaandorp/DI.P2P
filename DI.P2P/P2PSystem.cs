#pragma warning disable 1998
namespace DI.P2P
{
    using System;
    using System.Diagnostics;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    using Akka.Actor;
    using Akka.Configuration;
    using Akka.Configuration.Hocon;

    using DI.P2P.Connection;
    using DI.P2P.Messages;

    using Newtonsoft.Json.Bson;

    public class P2PSystem : IComponent
    {
        private readonly Peer selfPeer;

        private readonly string configurationDirectory;

        private readonly RSAParameters rsaParameters;

        private readonly string dataDirectory;

        private ActorSystem system;

        private IActorRef tcpServer;

        private IActorRef peerPool;

        private IActorRef peerRegistry;

        private IActorRef segmentManager;

        private IActorRef configuration;

        private IActorRef broadcastHandler;

        private IActorRef persistence;

        public P2PSystem(Module owner, Peer selfPeer, string configurationDirectory, RSAParameters rsaParameters, string dataDirectory)
        {
            this.selfPeer = selfPeer;
            this.configurationDirectory = configurationDirectory;
            this.rsaParameters = rsaParameters;
            this.dataDirectory = dataDirectory;
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
            this.configuration = this.system.ActorOf(Configuration.Props(this.configurationDirectory, this.rsaParameters, this.selfPeer), "Configuration");
            this.persistence = this.system.ActorOf(Persistence.Props(this.dataDirectory), "Persistence");
            this.broadcastHandler = this.system.ActorOf(BroadcastHandler.Props(), "BroadcastHandler");
            this.tcpServer = this.system.ActorOf(TcpServer.Props(), "TcpServer");
            this.peerPool = this.system.ActorOf(PeerPool.Props(), "PeerPool");
            this.peerRegistry = this.system.ActorOf(PeerRegistry.Props(), "PeerRegistry");
            this.segmentManager = this.system.ActorOf(SegmentManager.Props(), "SegmentManager");

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

        private (string ipAddress, int port) ParseIpAndPort(string node)
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

            return (ipAddress, port);
        }

        public void AddNode(string node)
        {
            var (ipAddress, port) = this.ParseIpAndPort(node);
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

        public BanInfo[] GetBannedPeers()
        {
            var response = this.peerRegistry.Ask<PeerRegistry.GetBannedPeersResponse>(new PeerRegistry.GetBannedPeers()).Result;
            return response.Peers;
        }

        public void BanPeer(string ipAddress, int port)
        {
            this.peerRegistry.Tell(new PeerRegistry.BanPeer(new Peer { IpAddress = ipAddress, Port = port }, DateTime.UtcNow.AddDays(1)));
        }

        private PeerInfo FindPeerInfo(Peer peer)
        {
            return this.peerRegistry.Ask<PeerInfo>(new PeerRegistry.FindPeerInfo(peer)).Result;
        }

        public TimeSpan Ping(string node)
        {
            var (ipAddress, port) = this.ParseIpAndPort(node);
            var peerInfo = this.FindPeerInfo(new Peer { IpAddress = ipAddress, Port = port });

            if (peerInfo == null)
            {
                throw new Exception("Peer not found.");
            }

            var sw = Stopwatch.StartNew();
            var result = peerInfo.ProtocolHandler.Ask<ProtocolHandler.ReceivePong>(new ProtocolHandler.SendPing(), TimeSpan.FromSeconds(5)).Result;

            if (!string.IsNullOrWhiteSpace(result.ErrorMsg))
            {
                throw new Exception(result.ErrorMsg);
            }

            return sw.Elapsed;
        }

        public void Broadcast(string testString)
        {
            var data = Encoding.UTF8.GetBytes(testString);
            this.broadcastHandler.Tell(new BroadcastHandler.SendBroadcast(data));
        }

        public void RegisterBroadcastHandler(Action<BroadcastMessage> handler)
        {
            this.broadcastHandler.Tell(new BroadcastHandler.RegisterHandler(handler));
        }
    }
}

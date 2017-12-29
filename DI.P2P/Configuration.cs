using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using System.IO;
    using System.Security.Cryptography;

    using Akka.Actor;
    using Akka.Event;

    using DI.P2P.Messages;

    using Newtonsoft.Json;

    public class Configuration : ReceiveActor
    {
        public class GetRsaParameters { }

        public class GetRsaParametersResponse
        {
            public GetRsaParametersResponse(RSAParameters rsaParameters)
            {
                this.RsaParameters = rsaParameters;
            }

            public RSAParameters RsaParameters { get; }
        }

        public class LoadPeers { }

        public class LoadPeersReponse
        {
            public LoadPeersReponse(Peer[] peers)
            {
                this.Peers = peers;
            }

            public Peer[] Peers { get; }
        }

        public class SavePeers
        {
            public Peer[] Peers { get; }

            public SavePeers(Peer[] peers)
            {
                this.Peers = peers;
            }
        }

        public class LoadBannedPeers { }

        public class LoadBannedPeersReponse
        {
            public LoadBannedPeersReponse(BanInfo[] peers)
            {
                this.Peers = peers;
            }

            public BanInfo[] Peers { get; }
        }

        public class SaveBannedPeers
        {
            public BanInfo[] Peers { get; }

            public SaveBannedPeers(BanInfo[] peers)
            {
                this.Peers = peers;
            }
        }

        public class GetSelf { }

        public class GetSelfResponse
        {
            public GetSelfResponse(Peer self)
            {
                this.Self = self;
            }

            public Peer Self { get; }
        }


        private readonly ILoggingAdapter log = Context.GetLogger();

        private readonly string configurationDirectory;

        private readonly RSAParameters rsaParameters;

        private readonly Peer selfPeer;

        public Configuration(string configurationDirectory, RSAParameters rsaParameters, Peer selfPeer)
        {
            this.configurationDirectory = configurationDirectory;
            this.rsaParameters = rsaParameters;
            this.selfPeer = selfPeer;

            if (!Directory.Exists(this.configurationDirectory))
            {
                var dirInfo = Directory.CreateDirectory(this.configurationDirectory);
                this.log.Info($"Created configuration directory {dirInfo.FullName}");
            }

            var selfOnDisk = this.LoadSelf();
            if (this.selfPeer.Id.Equals(Guid.Empty))
            {
                // Make sure we reuse the old and possibly already announced id.
                this.selfPeer.Id = selfOnDisk.Id;
            }

            if (this.selfPeer.Id.Equals(Guid.Empty))
            {
                // If there is no id set, generate a new.
                this.selfPeer.Id = Guid.NewGuid();
            }

            this.SaveSelf();

            this.Receive<GetRsaParameters>(_ => this.Sender.Tell(new GetRsaParametersResponse(this.rsaParameters)));

            this.Receive<LoadPeers>(loadPeers => this.ProcessLoadPeers(loadPeers));

            this.Receive<SavePeers>(savePeers => this.ProcessSavePeers(savePeers));

            this.Receive<LoadBannedPeers>(loadBannedPeers => this.ProcessLoadBannedPeers(loadBannedPeers));

            this.Receive<SaveBannedPeers>(saveBannedPeers => this.ProcessSaveBannedPeers(saveBannedPeers));

            this.Receive<GetSelf>(_ => this.Sender.Tell(new GetSelfResponse(this.selfPeer)));
        }

        private string GetPeersPath()
        {
            return Path.Combine(this.configurationDirectory, "peers.json");
        }

        private string GetBannedPeersPath()
        {
            return Path.Combine(this.configurationDirectory, "banned.json");
        }

        private string GetSelfPath()
        {
            return Path.Combine(this.configurationDirectory, "self.json");
        }

        private Peer LoadSelf()
        {
            if (!File.Exists(this.GetSelfPath()))
            {
                return new Peer();
            }

            try
            {
                var serializer = JsonSerializer.CreateDefault();

                using (var textReader = new StreamReader(this.GetSelfPath()))
                using (var jsonTextReader = new JsonTextReader(textReader))
                {
                    return serializer.Deserialize<Peer>(jsonTextReader);
                }
            }
            catch (Exception ex)
            {
                this.log.Error(ex, $"Exception while loading {this.GetSelfPath()}");
                return new Peer();
            }
        }

        private void SaveSelf()
        {
            var serializer = JsonSerializer.CreateDefault();

            using (var textWriter = new StreamWriter(this.GetSelfPath(), false))
            using (var jsonTextWriter = new JsonTextWriter(textWriter))
            {
                serializer.Serialize(jsonTextWriter, this.selfPeer);
            }
        }

        private void ProcessLoadPeers(LoadPeers loadPeers)
        {
            if (!File.Exists(this.GetPeersPath()))
            {
                this.Sender.Tell(new LoadPeersReponse(new Peer[] { }));
                return;
            }

            try
            {
                var serializer = JsonSerializer.CreateDefault();

                using (var textReader = new StreamReader(this.GetPeersPath()))
                using (var jsonTextReader = new JsonTextReader(textReader))
                {
                    var peers = serializer.Deserialize<Peer[]>(jsonTextReader);
                    this.Sender.Tell(new LoadPeersReponse(peers));
                }
            }
            catch(Exception ex)
            {
                this.log.Error(ex, $"Exception while loading {this.GetPeersPath()}");
                this.Sender.Tell(new LoadPeersReponse(new Peer[] { }));
            }
        }

        private void ProcessSavePeers(SavePeers savePeers)
        {
            var serializer = JsonSerializer.CreateDefault();

            using (var textWriter = new StreamWriter(this.GetPeersPath(), false))
            using (var jsonTextWriter = new JsonTextWriter(textWriter))
            {
                serializer.Serialize(jsonTextWriter, savePeers.Peers);
            }
        }

        private void ProcessLoadBannedPeers(LoadBannedPeers loadBannedPeers)
        {
            if (!File.Exists(this.GetBannedPeersPath()))
            {
                this.Sender.Tell(new LoadBannedPeersReponse(new BanInfo[] { }));
                return;
            }

            try
            {
                var serializer = JsonSerializer.CreateDefault();

                using (var textReader = new StreamReader(this.GetBannedPeersPath()))
                using (var jsonTextReader = new JsonTextReader(textReader))
                {
                    var peers = serializer.Deserialize<BanInfo[]>(jsonTextReader);
                    this.Sender.Tell(new LoadBannedPeersReponse(peers));
                }
            }
            catch (Exception ex)
            {
                this.log.Error(ex, $"Exception while loading {this.GetBannedPeersPath()}");
                this.Sender.Tell(new LoadBannedPeersReponse(new BanInfo[] { }));
            }
        }

        private void ProcessSaveBannedPeers(SaveBannedPeers saveBannedPeers)
        {
            var serializer = JsonSerializer.CreateDefault();

            using (var textWriter = new StreamWriter(this.GetBannedPeersPath(), false))
            using (var jsonTextWriter = new JsonTextWriter(textWriter))
            {
                serializer.Serialize(jsonTextWriter, saveBannedPeers.Peers);
            }
        }

        public static Props Props(string configurationDirectory, RSAParameters rsaParameters, Peer selfPeer)
        {
            return Akka.Actor.Props.Create(() => new Configuration(configurationDirectory, rsaParameters, selfPeer));
        }
    }
}

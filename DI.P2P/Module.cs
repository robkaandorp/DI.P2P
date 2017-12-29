using System;
using System.Collections.Generic;
using System.Linq;

namespace DI.P2P
{
    using System.Security.Cryptography;
    using System.Threading.Tasks;

    using DI.P2P.Messages;

    public class Module : IRunnable
    {
        protected log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(Module));

        private readonly List<IComponent> components = new List<IComponent>();

        private readonly RSAParameters rsaParameters;

        public Module(int port = 11111, string configurationDirectory = "conf")
        {
            this.Port = port;
            this.ConfigurationDirectory = configurationDirectory;

            var rsa = RSA.Create();
            this.rsaParameters = rsa.ExportParameters(true);
        }

        public Task Configure()
        {
            var selfPeer = new Peer
                               {
                                   Id = Guid.Empty, // Reuse old Id or generate a new.
                                   Port = this.Port,
                                   SoftwareVersion = Versions.SoftwareVersion,
                                   ProtocolVersion = Versions.ProtocolVersion,
                                   RsaParameters = new RsaParameters
                                                       {
                                                           Exponent = this.rsaParameters.Exponent,
                                                           Modulus = this.rsaParameters.Modulus
                                                       }
                               };
            return this.Add(new P2PSystem(this, selfPeer, this.ConfigurationDirectory, this.rsaParameters));
        }

        private async Task StartComponent(IComponent component)
        {
            this.Logger.InfoFormat("Starting component {0}...", component.GetType().Name);
            await component.Start();
            this.Logger.InfoFormat("... component {0} started.", component.GetType().Name);
        }

        private async Task StopComponent(IComponent component)
        {
            this.Logger.InfoFormat("Stopping component {0}...", component.GetType().Name);
            await component.Stop();
            this.Logger.InfoFormat("... component {0} stopped.", component.GetType().Name);
        }

        public async Task Start()
        {
            this.Logger.Info("Starting module...");

            var tasks = this.components
                .Where(c => !c.IsRunning)
                .Select(this.StartComponent)
                .ToArray();

            await Task.WhenAll(tasks);

            this.IsRunning = true;
            this.Logger.Info("... module started.");
        }

        public async Task Stop()
        {
            this.Logger.Info("Stopping module...");

            var tasks = this.components
                .Where(c => c.IsRunning)
                .Select(this.StopComponent)
                .ToArray();

            await Task.WhenAll(tasks);

            this.IsRunning = false;
            this.Logger.Info("... module stopped.");
        }

        public async Task Add(IComponent component)
        {
            this.components.Add(component);

            if (this.IsRunning && !component.IsRunning)
            {
                await this.StartComponent(component);
            }
            else if (!this.IsRunning && component.IsRunning)
            {
                await this.StopComponent(component);
            }
        }

        public bool IsRunning
        {
            get;
            protected set;
        }

        public T1 Find<T1>() where T1 : class
        {
            return this.components.Single(c => c is T1) as T1;
        }

        public Guid Id { get; set; }
        public int Port { get; set; }

        public string ConfigurationDirectory { get; }
    }
}

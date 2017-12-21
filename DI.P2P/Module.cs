using System;
using System.Collections.Generic;
using System.Linq;

namespace DI.P2P
{
    using System.Threading.Tasks;

    using DI.P2P.Messages;

    public class Module : IRunnable
    {
        protected log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(Module));

        private readonly List<IComponent> components = new List<IComponent>();

        public Module(Guid id, int port = 11111)
        {
            this.Id = id;
            this.Port = port;
        }

        public Task Configure()
        {
            var selfPeer = new Peer { Id = this.Id, Port = this.Port };
            return this.Add(new P2PSystem(this, selfPeer));
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
    }
}

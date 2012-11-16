using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DI.P2P
{
    public class Module : IRunnable
    {
        protected log4net.ILog Logger;
        private List<IComponent> _components = new List<IComponent>();

        public Module(Guid id, int port = 11111)
        {
            Id = id;
            Port = port;
            Logger = log4net.LogManager.GetLogger(GetType());
        }

        public void Configure()
        {
            Add(new ConnectionsComponent(this));
            Add(new NodesComponent(this));
            Add(new ServiceHostHandler(this));
            Add(new ServiceHostComponent(this));
            Add(new ClientInterface(this));
        }

        private void StartComponent(IComponent component)
        {
            Logger.InfoFormat("Starting component {0}...", component.GetType().Name);
            component.Start();
            Logger.InfoFormat("... component {0} started.", component.GetType().Name);
        }

        private void StopComponent(IComponent component)
        {
            Logger.InfoFormat("Stopping component {0}...", component.GetType().Name);
            component.Stop();
            Logger.InfoFormat("... component {0} stopped.", component.GetType().Name);
        }

        public void Start()
        {
            Logger.Info("Starting module...");
            _components
                .Where(c => !c.IsRunning)
                .AsParallel().ForAll(StartComponent);
            IsRunning = true;
            Logger.Info("... module started.");
        }

        public void Stop()
        {
            Logger.Info("Stopping module...");
            _components
                .Where(c => c.IsRunning)
                .AsParallel().ForAll(StopComponent);
            IsRunning = false;
            Logger.Info("... module stopped.");
        }

        public void Add(IComponent component)
        {
            _components.Add(component);

            if (IsRunning && !component.IsRunning)
            {
                StartComponent(component);
            }
            else if (!IsRunning && component.IsRunning)
            {
                StopComponent(component);
            }
        }

        public bool IsRunning
        {
            get;
            protected set;
        }

        public T1 Find<T1>() where T1 : class
        {
            return _components.Single(c => c is T1) as T1;
        }

        public Guid Id { get; set; }
        public int Port { get; set; }

        public ClientInterface ClientInterface
        {
            get
            {
                return Find<ClientInterface>();
            }
        }

        public Entities.Node GetMyNode()
        {
            return new Entities.Node()
            {
                Id = Id,
                Adresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName())
                                .Select(ip => ip.ToString())
                                .ToArray(),
                Port = Port,
                Connections = Find<ConnectionsComponent>().Connections.Count,
            };
        }
    }
}

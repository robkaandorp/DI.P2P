using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace DI.P2P
{
    public class ServiceHostComponent : IComponent
    {
        protected log4net.ILog Logger;
        private ServiceHost _serviceHost;

        public ServiceHostComponent(Module owner)
        {
            Owner = owner;
            Logger = log4net.LogManager.GetLogger(GetType());
        }

        public Module Owner
        {
            get;
            protected set;
        }

        public void Start()
        {
            _serviceHost = new ServiceHost(Owner.Find<ServiceHostHandler>(), new Uri("net.tcp://" + System.Net.Dns.GetHostName() + ":" + Owner.Port));
            
            var messageServiceEndpoint = 
                _serviceHost.AddServiceEndpoint(typeof(IMessage), new NetTcpBinding(), "Message");
            Logger.InfoFormat("Message endpoint listening at {0}", messageServiceEndpoint.ListenUri);

            var connectServiceEndpoint =
                _serviceHost.AddServiceEndpoint(typeof(IConnect), new NetTcpBinding(), "Connect");
            Logger.InfoFormat("Connect endpoint listening at {0}", connectServiceEndpoint.ListenUri);

            _serviceHost.Open();

            IsRunning = true;
        }

        public void Stop()
        {
            _serviceHost.Abort();
            _serviceHost.Close();
            IsRunning = false;
        }

        public bool IsRunning
        {
            get;
            protected set;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DI.P2P
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode=ConcurrencyMode.Multiple)]
    public partial class ServiceHostHandler : IComponent
    {
        protected log4net.ILog Logger;

        public ServiceHostHandler(Module owner)
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
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public bool IsRunning
        {
            get;
            protected set;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DI.P2P
{
    public class Connection
    {
        public Guid Id { get; set; }
        public IConnect ConnectChannel { get; set; }
        public IMessage MessageChannel { get; set; }
    }

    public class ConnectionsComponent : IComponent
    {
        private Dictionary<Guid, Connection> _connections = new Dictionary<Guid, Connection>();
        private object _connectionsLock = new object();

        public ConnectionsComponent(Module owner)
        {
            Owner = owner;
        }

        public List<Connection> Connections
        {
            get
            {
                lock (_connectionsLock)
                {
                    return _connections.Values.ToList();
                }
            }
        }

        public void AddConnection(Guid id, IConnect connectChannel, IMessage messageChannel)
        {
            lock (_connectionsLock)
            {
                var connection = new Connection()
                {
                    Id = id,
                    ConnectChannel = connectChannel,
                    MessageChannel = messageChannel
                };
                _connections.Add(id, connection);
            }
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

        public Connection GetConnection(Guid id)
        {
            lock (_connectionsLock)
            {
                return _connections[id];
            }
        }

        internal bool HasConnection(Guid guid)
        {
            lock (_connectionsLock)
            {
                return _connections.ContainsKey(guid);
            }
        }
    }
}

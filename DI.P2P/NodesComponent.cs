using DI.P2P.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DI.P2P
{
    public class NodesComponent : IComponent
    {
        protected log4net.ILog Logger;
        private Dictionary<Guid, Node> _nodes = new Dictionary<Guid, Node>();
        private object _nodeLock = new object();

        public NodesComponent(Module owner)
        {
            Logger = log4net.LogManager.GetLogger(GetType());
            Owner = owner;
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

        public IList<Node> Nodes
        {
            get
            {
                lock (_nodeLock)
                {
                    return _nodes.Values.ToList();
                }
            }
        }

        public void AddOrUpdateNode(Node node)
        {
            lock (_nodeLock)
            {
                if (!_nodes.ContainsKey(node.Id))
                {
                    Logger.DebugFormat("Information for {0} added.", node);
                    _nodes.Add(node.Id, node);
                }
                else
                {
                    Logger.DebugFormat("Information for {0} updated.", node);
                    _nodes[node.Id] = node;
                }
            }
        }

        public IList<Node> GetTopConnectedNodes(int number)
        {
            lock (_nodeLock)
            {
                return _nodes.Values
                    .OrderBy(n => n.Connections)
                    .Reverse()
                    .Take(number)
                    .ToList();
            }
        }

        public Node GetNode(Guid source)
        {
            lock (_nodeLock)
            {
                return _nodes[source];
            }
        }

        public bool Contains(Node node)
        {
            lock (_nodeLock)
            {
                return _nodes.ContainsKey(node.Id);
            }
        }

        public void Remove(Guid guid)
        {
            lock (_nodeLock)
            {
                _nodes.Remove(guid);
            }
        }
    }
}

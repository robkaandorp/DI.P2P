using DI.P2P.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DI.P2P
{
    public class ClientInterface : IComponent
    {
        DuplexChannelFactory<IConnect> _connectChannelFactory;
        ChannelFactory<IMessage> _messageChannelFactory;

        public ClientInterface(Module owner)
        {
            Owner = owner;
            _connectChannelFactory = new DuplexChannelFactory<IConnect>(
                Owner.Find<ServiceHostHandler>(), new NetTcpBinding());
            _messageChannelFactory = new ChannelFactory<IMessage>(
                new NetTcpBinding());
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

        public void Connect(string host, int port)
        {
            var nodesComponent = Owner.Find<NodesComponent>();

            var connectEP = new EndpointAddress(string.Format("net.tcp://{0}:{1}/Connect", host, port));
            var messageEP = new EndpointAddress(string.Format("net.tcp://{0}:{1}/Message", host, port));

            var connectChannel = _connectChannelFactory.CreateChannel(connectEP);
            var messageChannel = _messageChannelFactory.CreateChannel(messageEP);

            var response = connectChannel.Connect(new ConnectRequest()
            {
                Node = Owner.GetMyNode(),
                Nodes = nodesComponent.GetTopConnectedNodes(10),
            });

            Owner.Find<ConnectionsComponent>().AddConnection(response.Node.Id, connectChannel, messageChannel);
            nodesComponent.AddOrUpdateNode(response.Node);
            response.Nodes.ToList()
                .ForEach(nodesComponent.AddOrUpdateNode);
        }

        public event EventHandler<EventArgs<Message>> MessageReceived;

        public void OnMessageReceived(Message message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, new EventArgs<Message>(message));
            }
        }

        public void Ping(Guid id)
        {
            Owner.Find<ConnectionsComponent>()
                .GetConnection(id)
                .MessageChannel.Ping(Owner.Id, 1);
        }

        public event EventHandler<EventArgs<Tuple<Guid, int>>> AckReceived;

        public void OnAckReceived(Guid id, int number)
        {
            if (AckReceived != null)
            {
                var eventArgs = new EventArgs<Tuple<Guid, int>>(new Tuple<Guid, int>(id, number));
                AckReceived(this, eventArgs);
            }
        }

        public void Send(Message message)
        {
            var connectionsComponent = Owner.Find<ConnectionsComponent>();

            message.Path.Add(Owner.Id);

            if (message.To.HasValue && connectionsComponent.HasConnection(message.To.Value))
            {
                connectionsComponent
                    .GetConnection(message.To.Value).MessageChannel
                    .Send(message);
            }
            else
            {
                connectionsComponent
                    .Connections
                    .ForEach(c => c.MessageChannel.Send(message));
            }
        }
    }
}

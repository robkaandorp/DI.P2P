using DI.P2P.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DI.P2P
{
    public partial class ServiceHostHandler : IMessage
    {
        public void Ping(Guid source, int number)
        {
            Logger.DebugFormat("Ping received from {0}, nr. {1}.", source, number);
            var connection = Owner.Find<ConnectionsComponent>().GetConnection(source);
            connection.MessageChannel.Ack(Owner.Id, number);
        }

        public void Ack(Guid source, int number)
        {
            Logger.DebugFormat("Ack received from {0}, nr. {1}.", source, number);
            Owner.Find<ClientInterface>().OnAckReceived(source, number);
        }

        public void Send(Entities.Message message)
        {
            message.Path.Add(Owner.Id);

            // if message.To == null the message is a broadcast message and will be handled by each node and then forwarded
            if (message.To == null)
            {
                Handle(message);
                Forward(message);
            }
            else if (message.To == Owner.Id)
            {
                // message received
                Handle(message);
            }
            else
            {
                // forward message
                Forward(message);
            }
        }

        private void Handle(Message message)
        {
            Owner.Find<ClientInterface>().OnMessageReceived(message);
        }

        private void Forward(Message message)
        {
            var connectionsComponent = Owner.Find<ConnectionsComponent>();

            if (message.To.HasValue && connectionsComponent.HasConnection(message.To.Value))
            {
                connectionsComponent.GetConnection(message.To.Value)
                    .MessageChannel.Send(message);
            }
            else
            {
                connectionsComponent.Connections
                    .Where(c => !message.Path.Contains(c.Id))
                    .ToList()
                    .ForEach(c => c.MessageChannel.Send(message));
            }
        }

        public void Announce(AnnounceRequest announceRequest)
        {
            Node node = announceRequest.Node;
            AnnounceType announceType = announceRequest.AnnounceType;

            var nodesComponent = Owner.Find<NodesComponent>();
            var connectionsComponent = Owner.Find<ConnectionsComponent>();

            if (announceType == AnnounceType.Connect && !nodesComponent.Contains(node))
            {
                nodesComponent.AddOrUpdateNode(node);
                connectionsComponent.Connections
                    .ForEach(c => c.MessageChannel.Announce(CreateRequest(node, announceType)));
            }
            else if (announceType == AnnounceType.Update)
            {
                var oldNode = nodesComponent.GetNode(node.Id);
                if (oldNode == null || !oldNode.Equals(node))
                {
                    nodesComponent.AddOrUpdateNode(node);
                    connectionsComponent.Connections
                        .ForEach(c => c.MessageChannel.Announce(CreateRequest(node, announceType)));
                }
            }
            else if (announceType == AnnounceType.Disconnect)
            {
                var oldNode = nodesComponent.GetNode(node.Id);
                if (oldNode != null)
                {
                    nodesComponent.Remove(node.Id);
                    connectionsComponent.Connections
                        .ForEach(c => c.MessageChannel.Announce(CreateRequest(node, announceType)));
                }
            }
            else
            {
                Logger.WarnFormat("AnnounceType {0} is not implemented.", announceType);
            }
        }

        private AnnounceRequest CreateRequest(Node node, AnnounceType announceType)
        {
            return new AnnounceRequest()
            {
                Node = node,
                AnnounceType = announceType,
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DI.P2P
{
    public partial class ServiceHostHandler : IConnect, IComponent
    {
        public ConnectResponse Connect(ConnectRequest connectRequest)
        {
            Logger.DebugFormat("Connect received from {0}", connectRequest.Node);

            Owner.Find<ConnectionsComponent>()
                .AddConnection(connectRequest.Node.Id, null, Callback);

            var nodesComponent = Owner.Find<NodesComponent>();
            nodesComponent.AddOrUpdateNode(connectRequest.Node);
            connectRequest.Nodes.ToList()
                .ForEach(nodesComponent.AddOrUpdateNode);

            return new ConnectResponse()
                {
                    Node = Owner.GetMyNode(),
                    Nodes = nodesComponent.GetTopConnectedNodes(10)
                };
        }

        IMessage Callback
        {
            get
            {
                return OperationContext.Current.GetCallbackChannel<IMessage>();
            }
        }
    }
}

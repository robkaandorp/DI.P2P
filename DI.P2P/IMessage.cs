using DI.P2P.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DI.P2P
{
    [ServiceContract]
    public interface IMessage
    {
        [OperationContract(IsOneWay = true)]
        void Ping(Guid source, int number);

        [OperationContract(IsOneWay = true)]
        void Ack(Guid source, int number);

        [OperationContract(IsOneWay = true)]
        void Send(Message message);

        [OperationContract(IsOneWay = true)]
        void Announce(AnnounceRequest announceRequest);
    }
}

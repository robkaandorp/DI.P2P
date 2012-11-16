using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DI.P2P
{
    [ServiceContract(CallbackContract = typeof(IMessage))]
    public interface IConnect
    {
        [OperationContract]
        ConnectResponse Connect(ConnectRequest connectRequest);
    }
}

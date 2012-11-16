using DI.P2P.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DI.P2P
{
    public class ConnectRequest
    {
        public Node Node { get; set; }
        public IList<Node> Nodes { get; set; }
    }
}

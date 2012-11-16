using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DI.P2P.Entities;

namespace DI.P2P
{
    public class AnnounceRequest
    {
        public Node Node { get; set; }
        public AnnounceType AnnounceType { get; set; }
    }
}

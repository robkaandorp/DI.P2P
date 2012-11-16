using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DI.P2P.Entities
{
    [DataContract]
    public class Message
    {
        public Message()
        {
            Path = new List<Guid>();
        }

        [DataMember]
        public Node From { get; set; }

        [DataMember]
        public Guid? To { get; set; }
        
        [DataMember]
        public List<Guid> Path { get; protected set; }

        [DataMember]
        public byte[] Data { get; set; }
    }
}

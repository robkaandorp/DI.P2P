using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P.Messages
{
    using ProtoBuf;

    [ProtoContract]
    public class Peer
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }

        [ProtoMember(2)]
        public string IpAddress { get; set; }

        [ProtoMember(3)]
        public int Port { get; set; }

        [ProtoMember(4)]
        public DateTime AnnounceTime { get; set; }

        [ProtoMember(5)]
        public Version SoftwareVersion { get; set; }

        [ProtoMember(6)]
        public Version ProtocolVersion { get; set; }

        public override string ToString()
        {
            return $"{this.Id} {this.IpAddress}:{this.Port} {this.SoftwareVersion} protocol {this.ProtocolVersion}";
        }
    }
}

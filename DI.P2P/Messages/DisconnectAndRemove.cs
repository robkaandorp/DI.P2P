using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P.Messages
{
    using ProtoBuf;

    [ProtoContract]
    public class DisconnectAndRemove : Message
    {
        public const MessageEnum MessageType = MessageEnum.DisconnectAndRemove;

        [ProtoMember(1)]
        public string Reason { get; set; }

        [ProtoMember(2)]
        public Peer Peer { get; set; }

        public override MessageEnum GetMessageType()
        {
            return MessageType;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P.Messages
{
    using ProtoBuf;

    [ProtoContract]
    public class Ping : Message
    {
        public const MessageEnum MessageType = MessageEnum.Ping;

        [ProtoMember(1)]
        public string Data { get; } = "ping";

        public override MessageEnum GetMessageType()
        {
            return MessageType;
        }
    }
}

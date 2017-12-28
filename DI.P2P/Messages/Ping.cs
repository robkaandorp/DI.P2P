using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P.Messages
{
    using Akka.Actor;

    using ProtoBuf;

    [ProtoContract]
    public class Ping : Message
    {
        public const MessageEnum MessageType = MessageEnum.Ping;

        public Ping() { }

        public Ping(byte[] data, string tellPath)
        {
            this.Data = data;
            this.TellPath = tellPath;
        }

        [ProtoMember(1)]
        public byte[] Data { get; }

        [ProtoMember(2)]
        public string TellPath { get; }

        public override MessageEnum GetMessageType()
        {
            return MessageType;
        }
    }
}

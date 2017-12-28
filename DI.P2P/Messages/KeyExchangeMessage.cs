namespace DI.P2P.Messages
{
    using System;

    using ProtoBuf;

    [ProtoContract]
    public class KeyExchangeMessage : Message
    {
        public const MessageEnum MessageType = MessageEnum.KeyExchange;

        [ProtoMember(1)]
        public byte[] Key { get; set; }

        public override MessageEnum GetMessageType()
        {
            return MessageType;
        }
    }
}

namespace DI.P2P.Messages
{
    using System;

    using ProtoBuf;

    [ProtoContract]
    public class BroadcastMessage : Message
    {
        public const MessageEnum MessageType = MessageEnum.Broadcast;

        [ProtoMember(1)]
        public Guid Id { get; set; }

        [ProtoMember(2)]
        public Guid From { get; set; }

        [ProtoMember(3)]
        public byte[] Data { get; set; }

        public override MessageEnum GetMessageType()
        {
            return MessageType;
        }
    }
}

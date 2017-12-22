namespace DI.P2P.Messages
{
    using System;

    using ProtoBuf;

    [ProtoContract]
    public class AnnounceMessage : Message
    {
        public const MessageEnum MessageType = MessageEnum.Announce;

        [ProtoMember(1)]
        public Peer Peer { get; set; }

        [ProtoMember(2)]
        public Peer[] Peers { get; set; }

        [ProtoMember(3)]
        public DateTime MyTime { get; set; }

        public override MessageEnum GetMessageType()
        {
            return MessageType;
        }
    }
}

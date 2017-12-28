namespace DI.P2P.Messages
{
    using ProtoBuf;

    [ProtoContract]
    public class Pong : Message
    {
        public const MessageEnum MessageType = MessageEnum.Pong;

        [ProtoMember(1)]
        public string Data { get; } = "pong";

        public override MessageEnum GetMessageType()
        {
            return MessageType;
        }
    }
}
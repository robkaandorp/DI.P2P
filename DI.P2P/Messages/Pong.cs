namespace DI.P2P.Messages
{
    using Akka.Actor;

    using ProtoBuf;

    [ProtoContract]
    public class Pong : Message
    {
        public Pong() { }

        public Pong(byte[] data, string tellPath)
        {
            this.Data = data;
            this.TellPath = tellPath;
        }

        public const MessageEnum MessageType = MessageEnum.Pong;

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
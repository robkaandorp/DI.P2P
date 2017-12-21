namespace DI.P2P.Messages
{
    using ProtoBuf;

    [ProtoContract]
    public class Version
    {
        [ProtoMember(1)]
        public int Major { get; set; }

        [ProtoMember(2)]
        public int Minor { get; set; }

        [ProtoMember(3)]
        public int Build { get; set; }

        [ProtoMember(4)]
        public string Name { get; set; }

        public override string ToString()
        {
            return $"v{this.Major}.{this.Minor}.{this.Build}-{this.Name}";
        }
    }
}
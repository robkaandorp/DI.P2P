namespace DI.P2P.Segment
{
    using Akka.Actor;

    public class Segment : ReceiveActor
    {
        private readonly string name;

        private readonly IActorRef configuration;

        private readonly IActorRef fileManager;

        public Segment(string name)
        {
            this.name = name;

            this.configuration = Context.ActorOf(Configuration.Props());
            this.fileManager = Context.ActorOf(FileManager.Props());
        }

        public static Props Props(string name)
        {
            return Akka.Actor.Props.Create(() => new Segment(name));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using Akka.Actor;

    public class SegmentManager : ReceiveActor
    {
        public class CreateSegment
        {
            public string Name { get; set; }
        }


        public SegmentManager()
        {
            this.Receive<CreateSegment>(createSegment => this.ProcessCreateSegment(createSegment));
        }

        private void ProcessCreateSegment(CreateSegment createSegment)
        {
            Context.ActorOf(Segment.Segment.Props(createSegment.Name), createSegment.Name);
        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new SegmentManager());
        }
    }
}

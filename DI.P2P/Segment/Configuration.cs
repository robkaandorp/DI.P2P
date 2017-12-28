using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P.Segment
{
    using Akka.Actor;

    public class Configuration : ReceiveActor
    {
        public Configuration()
        {

        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new Configuration());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P.Segment
{
    using Akka.Actor;

    public class FileManager : ReceiveActor
    {
        public FileManager()
        {

        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new FileManager());
        }
    }
}

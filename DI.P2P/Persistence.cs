using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using System.IO;

    using Akka.Actor;
    using Akka.Event;

    public class Persistence : ReceiveActor
    {
        private readonly string dataDirectory;

        private readonly ILoggingAdapter log = Context.GetLogger();

        public Persistence(string dataDirectory)
        {
            this.dataDirectory = dataDirectory;

            if (!Directory.Exists(this.dataDirectory))
            {
                var dirInfo = Directory.CreateDirectory(this.dataDirectory);
                this.log.Info($"Created data directory {dirInfo.FullName}");
            }
        }

        public static Props Props(string dataDirectory)
        {
            return Akka.Actor.Props.Create(() => new Persistence(dataDirectory));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    public class Versions
    {
        public const byte TransportLayer = 1;

        public static readonly Messages.Version SoftwareVersion =
            new Messages.Version { Major = 0, Minor = 0, Build = 1, Name = "alpha" };

        public static readonly Messages.Version ProtocolVersion =
            new Messages.Version { Major = 0, Minor = 0, Build = 1, Name = "alpha" };
    }
}

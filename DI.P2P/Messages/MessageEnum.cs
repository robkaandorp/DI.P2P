using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P.Messages
{
    public enum MessageEnum : byte
    {
        Announce = 1,
        DisconnectAndRemove = 2,
        KeyExchange = 3,
        Broadcast = 4,
        Ping = 5,
        Pong = 6
    }
}

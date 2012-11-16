using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DI.P2P
{
    public class EventArgs<T>
    {
        public EventArgs(T data)
        {
            Data = data;
        }

        public T Data { get; set; }

    }
}

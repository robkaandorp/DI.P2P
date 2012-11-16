using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DI.P2P
{
    public interface IRunnable
    {
        void Start();
        void Stop();
        bool IsRunning { get; }
    }
}

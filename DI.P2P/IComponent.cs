﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DI.P2P
{
    public interface IComponent : IRunnable
    {
        Module Owner { get; }
    }
}

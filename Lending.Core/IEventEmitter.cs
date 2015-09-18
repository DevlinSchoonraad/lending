﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lending.Core
{
    public interface IEventEmitter<in T> where T : Event
    {
        void EmitEvent(T t);
    }
}
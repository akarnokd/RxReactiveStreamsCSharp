﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    public interface ISubscription
    {
        void Request(long n);

        void Cancel();
    }
}
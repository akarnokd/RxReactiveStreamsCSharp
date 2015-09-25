using RxReactiveStreamsCSharp.Internal.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    public static class Schedulers
    {
        static readonly ThreadPoolScheduler DEFAULT = new ThreadPoolScheduler();

        public static Scheduler Default { get { return DEFAULT; } }
    }
}

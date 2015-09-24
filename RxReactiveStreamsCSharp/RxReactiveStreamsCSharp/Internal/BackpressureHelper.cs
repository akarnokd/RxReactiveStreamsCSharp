using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal
{
    public static class BackpressureHelper
    {
        public static long addCap(long a, long b)
        {
            long u = a + b;
            if (u < 0L)
            {
                u = long.MaxValue;
            }
            return u;
        }

        public static long add(ref long reference, long n)
        {
            for (;;)
            {
                long r = Volatile.Read(ref reference);
                long u = addCap(r, n);
                if (Interlocked.CompareExchange(ref reference, u, r) == r)
                {
                    return r;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Queue
{
    static class QueueHelper
    {
        public static int Round2(int x)
        {
            x--; // comment out to always take the next biggest power of two, even if x is already a power of two
            x |= (x >> 1);
            x |= (x >> 2);
            x |= (x >> 4);
            x |= (x >> 8);
            x |= (x >> 16);
            return (x + 1);
        }

        public static bool IsPowerOfTwo(int value)
        {
            return (value & (value - 1)) == 0;
        }

        public static int Size(ref long producerIndex, ref long consumerIndex)
        {
            long ci = Volatile.Read(ref consumerIndex);
            for (;;)
            {
                long pi = Volatile.Read(ref producerIndex);

                long ci2 = Volatile.Read(ref consumerIndex);

                if (ci == ci2)
                {
                    return (int)(pi - ci);
                }
                ci = ci2;
            }
        }
    }
}

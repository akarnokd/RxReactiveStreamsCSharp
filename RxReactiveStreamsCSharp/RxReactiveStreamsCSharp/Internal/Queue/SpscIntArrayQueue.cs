using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Queue
{
    public sealed class SpscIntArrayQueue : IStructuredQueue<int>
    {
        readonly long[] array;
        readonly int mask;

        long producerIndex;

        long consumerIndex;

        public SpscIntArrayQueue(int capacity)
        {
            int c = QueueHelper.Round2(capacity);
            this.array = new long[c];
            this.mask = c - 1;
        }

        public void Clear()
        {
            int dummy;
            while (TryPoll(out dummy) && !IsEmpty()) ;
        }

        public bool IsEmpty()
        {
            return Volatile.Read(ref producerIndex) == Volatile.Read(ref consumerIndex);
        }

        public bool Offer(int value)
        {
            long[] a = array;
            long pi = producerIndex;
            int m = mask;

            int offset = (int)pi & m;

            if ((Volatile.Read(ref a[offset]) & 0x100000000L) != 0L) {
                return false;
            }

            Volatile.Write(ref producerIndex, pi + 1);
            Volatile.Write(ref a[offset], (long)value | 0x100000000L);
            return true;
        }

        public int Size()
        {
            return QueueHelper.Size(ref producerIndex, ref consumerIndex);
        }

        public bool TryPeek(out int value)
        {
            long[] a = array;
            long pi = consumerIndex;
            int m = mask;

            int offset = (int)pi & m;

            long v = Volatile.Read(ref a[offset]);

            if ((v & 0x100000000L) != 0L)
            {
                value = (int)(v & 0xFFFFFFFFL);
                return true;
            }
            value = 0;
            return false;
        }

        public bool TryPoll(out int value)
        {
            long[] a = array;
            long ci = consumerIndex;
            int m = mask;

            int offset = (int)ci & m;

            long v = Volatile.Read(ref a[offset]);

            if ((v & 0x100000000L) != 0L)
            {
                value = (int)(v & 0xFFFFFFFFL);

                Volatile.Write(ref consumerIndex, ci + 1);
                Volatile.Write(ref a[offset], 0L);

                return true;
            }
            value = 0;
            return false;
        }
    }
}

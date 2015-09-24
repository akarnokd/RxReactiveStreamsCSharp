using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Queue
{
    public sealed class SpscLinkedArrayQueue<T> where T : class
    {

        long producerIndex;
        object[] producerArray;

        long consumerIndex;
        object[] consumerArray;

        readonly int mask;

        static readonly object NEXT = new object();

        public SpscLinkedArrayQueue(int capacity)
        {
            int c = QueueHelper.round2(capacity);

            mask = c - 1;

            producerArray = new object[c + 1];
            consumerArray = producerArray;
        }

        public bool Offer(T value)
        {
            object[] a = producerArray;
            long pi = producerIndex;
            int m = mask;

            int offset1 = (int)(pi + 1) & m;

            if (Volatile.Read(ref a[offset1]) == null)
            {
                int offset = (int)pi & m;
                Volatile.Write(ref producerIndex, pi + 1);
                Volatile.Write(ref a[offset], value);
            } else
            {
                object[] b = new object[m + 2];

                int offset = (int)pi & m;
                b[offset] = value;
                a[m + 1] = b;
                producerArray = b;

                Volatile.Write(ref producerIndex, pi + 1);
                Volatile.Write(ref a[offset], NEXT);
            }

            return true;
        }

        public Boolean Offer(T first, T second)
        {
            object[] a = producerArray;
            long pi = producerIndex;
            int m = mask;

            int offset2 = (int)(pi + 2) & m;

            if (Volatile.Read(ref a[offset2]) == null)
            {
                int offset = (int)pi & m;

                Volatile.Write(ref a[offset + 1], second);
                Volatile.Write(ref producerIndex, pi + 2);
                Volatile.Write(ref a[offset], first);
            } else
            {
                object[] b = new object[m + 2];

                int offset = (int)pi & m;

                b[offset] = first;
                b[offset + 1] = second;
                a[mask + 1] = b;
                producerArray = b;

                Volatile.Write(ref producerIndex, pi + 2);
                Volatile.Write(ref a[offset], NEXT);
            }
            return true;
        }

        public T Peek()
        {
            object[] a = consumerArray;
            long ci = consumerIndex;
            int m = mask;

            int offset = (int)ci & m;

            object o = Volatile.Read(ref a[offset]);
            if (o != null)
            {
                if (o == NEXT)
                {
                    a = (object[])a[mask + 1];
                    consumerArray = a;
                    return (T)Volatile.Read(ref a[offset]);
                }
                return (T)o;
            }
            return null;
        }

        public T Poll()
        {
            object[] a = consumerArray;
            long ci = consumerIndex;
            int m = mask;

            int offset = (int)ci & m;

            object o = Volatile.Read(ref a[offset]);

            if (o != null)
            {
                if (o == NEXT)
                {
                    a = (object[])a[mask + 1];
                    consumerArray = a;
                    o = Volatile.Read(ref a[offset]);
                }
                Volatile.Write(ref consumerIndex, ci + 1);
                Volatile.Write(ref a[offset], null);

                return (T)o;
            }
            return null;
        }

        public bool IsEmpty()
        {
            return Volatile.Read(ref producerIndex) == Volatile.Read(ref consumerIndex);
        }

        public int Size()
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

        public void Clear()
        {
            while (Poll() != null || !IsEmpty()) ;
        }
    }
}

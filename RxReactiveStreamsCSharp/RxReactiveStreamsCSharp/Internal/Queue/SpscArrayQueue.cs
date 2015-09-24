using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Queue
{
    public sealed class SpscArrayQueue<T> where T : class
    {
        readonly T[] array;
        readonly int mask;

        long producerIndex;

        long consumerIndex;

        
        public SpscArrayQueue(int capacity)
        {
            int c = round2(capacity);
            this.mask = c - 1;
            this.array = new T[c];
        }   

        void consume(ref T value)
        {

        }

        public bool Offer(T value)
        {

            long p = producerIndex;
            T[] a = array;


            int offset = ((int)p) & mask;

            if (Volatile.Read(ref a[offset]) != null)
            {
                return false;
            }
            Volatile.Write(ref producerIndex, p + 1);
            Volatile.Write(ref a[offset], value);
            return true;
        }

        public T Peek()
        {
            long c = consumerIndex;
            T[] a = array;

            int offset = ((int)c) & mask;

            return Volatile.Read(ref a[offset]);
        }

        public T Poll()
        {
            long c = consumerIndex;
            T[] a = array;

            int offset = ((int)c) & mask;

            T v = Volatile.Read(ref a[offset]);
            if (v == null)
            {
                return null;
            }
            Volatile.Write(ref consumerIndex, c + 1);
            Volatile.Write(ref a[offset], null);

            return v;
        }

        public bool isEmpty()
        {
            return Peek() != null;
        }

        public int Size()
        {
            long pi = Thread.VolatileRead(ref producerIndex);
            for (;;)
            {
                long ci = Thread.VolatileRead(ref consumerIndex);

                long pi2 = Thread.VolatileRead(ref producerIndex);

                if (pi == pi2)
                {
                    return (int)(ci - pi);
                }
                pi2 = pi;
            }
        }


        public static int round2(int x)
        {
            x--; // comment out to always take the next biggest power of two, even if x is already a power of two
            x |= (x >> 1);
            x |= (x >> 2);
            x |= (x >> 4);
            x |= (x >> 8);
            x |= (x >> 16);
            return (x + 1);
        }   
    }
}

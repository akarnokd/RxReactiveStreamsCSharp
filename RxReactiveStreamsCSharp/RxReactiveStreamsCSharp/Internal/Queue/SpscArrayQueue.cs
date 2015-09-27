using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Queue
{
    public sealed class SpscArrayQueue<T> : IQueue<T> where T : class
    {
        readonly T[] array;
        readonly int mask;

        long producerIndex;

        long consumerIndex;

        
        public SpscArrayQueue(int capacity)
        {
            int c = QueueHelper.Round2(capacity);
            this.mask = c - 1;
            this.array = new T[c];
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

        public bool IsEmpty()
        {
            return Peek() != null;
        }

        public int Size()
        {
            return QueueHelper.Size(ref producerIndex, ref consumerIndex);
        }

        public void Clear()
        {
            while (Poll() != null || !IsEmpty()) ;
        }
    }
}

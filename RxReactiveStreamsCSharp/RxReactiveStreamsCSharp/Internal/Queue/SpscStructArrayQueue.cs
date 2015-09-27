using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Queue
{
    public sealed class SpscStructArrayQueue<T> : IStructuredQueue<T> where T : struct
    {
        internal struct QueueCell {
            internal T value;
            internal bool occupied;
        }

        readonly QueueCell[] array;
        readonly int mask;

        long producerIndex;
        long consumerIndex;

        public SpscStructArrayQueue(int capacity)
        {
            int c = QueueHelper.Round2(capacity);
            array = new QueueCell[c];
            mask = c - 1;
        }

        public bool Offer(T value)
        {
            QueueCell[] a = array;
            int m = mask;
            long pi = producerIndex;

            int offset = (int)pi & m;

            QueueCell qc = a[offset];

            if (Volatile.Read(ref qc.occupied))
            {
                return false;
            }

            qc.value = value;

            Volatile.Write(ref producerIndex, pi + 1);
            Volatile.Write(ref qc.occupied, true);
            return true;
        }

        public bool TryPeek(out T value)
        {
            QueueCell[] a = array;
            int m = mask;
            long ci = consumerIndex;

            int offset = (int)ci & m;

            QueueCell qc = a[offset];
            if (Volatile.Read(ref qc.occupied))
            {
                value = qc.value;
                return true;
            }

            value = default(T);
            return false;
        }

        public bool TryPoll(out T value)
        {
            QueueCell[] a = array;
            int m = mask;
            long ci = consumerIndex;

            int offset = (int)ci & m;

            QueueCell qc = a[offset];
            if (Volatile.Read(ref qc.occupied))
            {
                value = qc.value;

                value = default(T);

                Volatile.Write(ref consumerIndex, ci + 1);
                Volatile.Write(ref qc.occupied, false);

                return true;
            }

            value = default(T);
            return false;
        }

        public int Size()
        {
            return QueueHelper.Size(ref producerIndex, ref consumerIndex);
        }

        public bool IsEmpty()
        {
            return Volatile.Read(ref producerIndex) == Volatile.Read(ref consumerIndex);
        }

        public void Clear()
        {
            T dummy;
            while (TryPoll(out dummy) && !IsEmpty()) ;
        }
    }
}

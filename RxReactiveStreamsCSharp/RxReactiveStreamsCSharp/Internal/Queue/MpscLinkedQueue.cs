using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Queue
{
    public sealed class MpscLinkedQueue<T> : IQueue<T> where T : class
    {
        internal sealed class LinkedNode
        {
            internal T value;
            internal LinkedNode next;

            public LinkedNode(T value)
            {
                this.value = value;
            }
        }

        LinkedNode head;
        LinkedNode tail;

        public MpscLinkedQueue()
        {
            head = new LinkedNode(null);

            Volatile.Write(ref tail, head);
        }

        public bool Offer(T value)
        {
            LinkedNode n = new LinkedNode(value);

            LinkedNode prev = Interlocked.Exchange(ref tail, n);

            Volatile.Write(ref n.next, prev);

            return true;
        }

        public T Peek()
        {
            LinkedNode h = head;

            LinkedNode n = Volatile.Read(ref h.next);

            if (n != null)
            {
                return n.value;
            }

            if (h != Volatile.Read(ref tail))
            {
                while ((n = Volatile.Read(ref h.next)) != null) ;

                return n.value;
            }

            return null;
        }

        public T Poll()
        {
            LinkedNode h = head;

            LinkedNode n = Volatile.Read(ref h.next);

            if (n != null)
            {
                T v = n.value;
                n.value = null;

                head = n;

                return v;
            }

            if (h != Volatile.Read(ref tail))
            {
                while ((n = Volatile.Read(ref h.next)) != null) ;

                T v = n.value;
                n.value = null;

                head = n;

                return v;
            }

            return null;
        }

        public int Size()
        {
            LinkedNode h = Volatile.Read(ref head);
            LinkedNode t = Volatile.Read(ref tail);

            int size = 0;

            while (h != t && size < int.MaxValue)
            {
                LinkedNode n;

                while ((n = Volatile.Read(ref h.next)) != null) ;

                h = n;

                size++;
            }

            return size;
        }

        public bool IsEmpty()
        {
            return Volatile.Read(ref head) == Volatile.Read(ref tail);
        }

        public void Clear()
        {
            while (Poll() != null && !IsEmpty()) ;
        }
    }

    public sealed class MpscStructLinkedQueue<T> : IStructuredQueue<T> where T : struct
    {
        internal sealed class LinkedNode
        {
            internal T value;
            internal LinkedNode next;

            public LinkedNode(T value)
            {
                this.value = value;
            }
        }

        LinkedNode head;
        LinkedNode tail;

        public MpscStructLinkedQueue()
        {
            head = new LinkedNode(default(T));

            Volatile.Write(ref tail, head);
        }

        public bool Offer(T value)
        {
            LinkedNode n = new LinkedNode(value);

            LinkedNode prev = Interlocked.Exchange(ref tail, n);

            Volatile.Write(ref n.next, prev);

            return true;
        }

        public bool TryPeek(out T value)
        {
            LinkedNode h = head;

            LinkedNode n = Volatile.Read(ref h.next);

            if (n != null)
            {
                value = n.value;
                return true;
            }

            if (h != Volatile.Read(ref tail))
            {
                while ((n = Volatile.Read(ref h.next)) != null) ;

                value = n.value;
                return true;
            }

            value = default(T);
            return false;
        }

        public bool TryPoll(out T value)
        {
            LinkedNode h = head;

            LinkedNode n = Volatile.Read(ref h.next);

            if (n != null)
            {
                T v = n.value;
                n.value = default(T);

                head = n;

                value = v;
                return true;
            }

            if (h != Volatile.Read(ref tail))
            {
                while ((n = Volatile.Read(ref h.next)) != null) ;

                T v = n.value;
                n.value = default(T);

                head = n;

                value = v;
                return true;
            }

            value = default(T);
            return false;
        }

        public int Size()
        {
            LinkedNode h = Volatile.Read(ref head);
            LinkedNode t = Volatile.Read(ref tail);

            int size = 0;

            while (h != t && size < int.MaxValue)
            {
                LinkedNode n;

                while ((n = Volatile.Read(ref h.next)) != null) ;

                h = n;

                size++;
            }

            return size;
        }

        public bool IsEmpty()
        {
            return Volatile.Read(ref head) == Volatile.Read(ref tail);
        }

        public void Clear()
        {
            T dummy;
            while (TryPoll(out dummy) && !IsEmpty()) ;
        }
    }
}

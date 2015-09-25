using RxReactiveStreamsCSharp.Internal.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Operators
{
    public sealed class OperatorOnBackpressureBuffer<T> : IFlowableOperator<T, T> where T : class
    {
        readonly int maxCapacity;
        readonly bool unbounded;
        readonly Action onOverflow;

        public OperatorOnBackpressureBuffer(int maxCapacity, bool unbounded, Action onOverflow)
        {
            this.maxCapacity = maxCapacity;
            this.unbounded = unbounded;
            this.onOverflow = onOverflow;
        }

        public ISubscriber<T> Invoke(ISubscriber<T> child)
        {
            if (unbounded)
            {
                return new BufferUnboundedSubscriber(child, maxCapacity);
            }
            return new BufferBoundedSubscriber(child, maxCapacity, onOverflow);
        }

        sealed class BufferUnboundedSubscriber : ISubscriber<T>, ISubscription
        {
            readonly ISubscriber<T> actual;
            readonly SpscLinkedArrayQueue<T> queue;

            volatile bool done;
            Exception error;

            ISubscription s;

            long requested;

            int wip;

            volatile bool cancelled;

            public BufferUnboundedSubscriber(ISubscriber<T> actual, int capacityHint)
            {
                this.actual = actual;
                this.queue = new SpscLinkedArrayQueue<T>(capacityHint);
            }

            public void Cancel()
            {
                if (!cancelled)
                {
                    cancelled = true;
                    if (Interlocked.Increment(ref wip) == 1)
                    {
                        queue.Clear();
                        s.Cancel();
                    }
                }
            }

            public void OnComplete()
            {
                if (done)
                {
                    return;
                }
                done = true;
                drain();
            }

            public void OnError(Exception e)
            {
                if (done)
                {
                    return;
                }
                error = e;
                done = true;
                drain();
            }

            public void OnNext(T t)
            {
                queue.Offer(t);
                drain();
            }

            public void OnSubscribe(ISubscription s)
            {
                this.s = s;
                actual.OnSubscribe(s);
                s.Request(long.MaxValue);
            }

            public void Request(long n)
            {
                if (n <= 0)
                {
                    return;
                }
                BackpressureHelper.add(ref requested, n);
                drain();
            }

            void drain()
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                int missed = 1;

                SpscLinkedArrayQueue<T> q = queue;
                ISubscriber<T> a = actual;

                for (;;)
                {
                    if (checkTerminated(done, q.IsEmpty(), a))
                    {
                        return;
                    }

                    long r = Volatile.Read(ref requested);
                    bool unbounded = r == long.MaxValue;
                    long e = 0L;

                    while (r != 0L)
                    {
                        bool d = done;
                        T v = q.Poll();
                        bool empty = v == null;

                        if (checkTerminated(d, empty, a))
                        {
                            return;
                        }

                        if (empty)
                        {
                            break;
                        }

                        a.OnNext(v);

                        r--;
                        e--;
                    }

                    if (!unbounded)
                    {
                        if (e != 0L)
                        {
                            Interlocked.Add(ref requested, e);
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            bool checkTerminated(bool d, bool empty, ISubscriber<T> a)
            {
                if (cancelled)
                {
                    s.Cancel();
                    queue.Clear();
                    return true;
                }

                if (d)
                {
                    Exception e = error;
                    if (e != null)
                    {
                        a.OnError(e);
                        return true;
                    } else
                    if (empty)
                    {
                        a.OnComplete();
                        return true;
                    }
                }
                return false;
            }
        }

        sealed class BufferBoundedSubscriber : ISubscriber<T>, ISubscription
        {
            readonly ISubscriber<T> actual;
            readonly SpscExactArrayQueue<T> queue;
            readonly Action onOverflow;

            volatile bool done;
            Exception error;

            ISubscription s;

            long requested;

            int wip;

            volatile bool cancelled;

            public BufferBoundedSubscriber(ISubscriber<T> actual, int capacity, Action onOverflow)
            {
                this.actual = actual;
                this.queue = new SpscExactArrayQueue<T>(capacity);
                this.onOverflow = onOverflow;
            }

            public void Cancel()
            {
                if (!cancelled)
                {
                    cancelled = true;
                    if (Interlocked.Increment(ref wip) == 1)
                    {
                        queue.Clear();
                        s.Cancel();
                    }
                }
            }

            public void OnComplete()
            {
                if (done)
                {
                    return;
                }
                done = true;
                drain();
            }

            public void OnError(Exception e)
            {
                if (done)
                {
                    return;
                }
                error = e;
                done = true;
                drain();
            }

            public void OnNext(T t)
            {
                if (!queue.Offer(t))
                {
                    s.Cancel();

                    try {
                        onOverflow();
                    } catch (Exception ex)
                    {
                        OnError(ex);
                        return;
                    }
                    OnError(new MissingBackpressureException());
                    return;
                }
                drain();
            }

            public void OnSubscribe(ISubscription s)
            {
                this.s = s;
                actual.OnSubscribe(s);
                s.Request(long.MaxValue);
            }

            public void Request(long n)
            {
                if (n <= 0)
                {
                    return;
                }
                BackpressureHelper.add(ref requested, n);
                drain();
            }

            void drain()
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                int missed = 1;

                SpscExactArrayQueue<T> q = queue;
                ISubscriber<T> a = actual;

                for (;;)
                {
                    if (checkTerminated(done, q.IsEmpty(), a))
                    {
                        return;
                    }

                    long r = Volatile.Read(ref requested);
                    bool unbounded = r == long.MaxValue;
                    long e = 0L;

                    while (r != 0L)
                    {
                        bool d = done;
                        T v = q.Poll();
                        bool empty = v == null;

                        if (checkTerminated(d, empty, a))
                        {
                            return;
                        }

                        if (empty)
                        {
                            break;
                        }

                        a.OnNext(v);

                        r--;
                        e--;
                    }

                    if (!unbounded)
                    {
                        if (e != 0L)
                        {
                            Interlocked.Add(ref requested, e);
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            bool checkTerminated(bool d, bool empty, ISubscriber<T> a)
            {
                if (cancelled)
                {
                    s.Cancel();
                    queue.Clear();
                    return true;
                }

                if (d)
                {
                    Exception e = error;
                    if (e != null)
                    {
                        a.OnError(e);
                        return true;
                    }
                    else
                    if (empty)
                    {
                        a.OnComplete();
                        return true;
                    }
                }
                return false;
            }
        }
    }
}

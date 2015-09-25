using RxReactiveStreamsCSharp;
using RxReactiveStreamsCSharp.Internal.Queue;
using RxReactiveStreamsCSharp.Internal.Subscriptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Operators
{
    public sealed class OperatorFlatMap<T, U> : IFlowableOperator<T, U> where U : class {
        readonly Func<T, IFlowable<U>> mapper;
        readonly bool delayErrors;
        readonly int maxConcurrency;
        readonly int bufferSize;

        public OperatorFlatMap( 
            Func<T, IFlowable<U>> mapper,
            bool delayErrors, int maxConcurrency, int bufferSize) {
            this.mapper = mapper;
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
            this.bufferSize = bufferSize;
        }

        public ISubscriber<T> Invoke(ISubscriber<U> t) {
            return new MergeSubscriber(t, mapper, delayErrors, maxConcurrency, bufferSize);
        }

        sealed class MergeSubscriber : ISubscription, ISubscriber<T> {

            readonly ISubscriber<U> actual;
            readonly Func<T, IFlowable<U>> mapper;
            readonly bool delayErrors;
            readonly int maxConcurrency;
            internal readonly int bufferSize;

            volatile IQueue<U> queue;

            volatile bool done;

            volatile ConcurrentQueue<Exception> errors;
        
            volatile bool cancelled;

            volatile InnerSubscriber[] subscribers;
        
            static readonly InnerSubscriber[] EMPTY = new InnerSubscriber[0];

            static readonly InnerSubscriber[] CANCELLED = new InnerSubscriber[0];

            long requested;

            volatile int wip;
        
            ISubscription s;

            long uniqueId;
            long lastId;
            int lastIndex;

            public MergeSubscriber(ISubscriber<U> actual, Func<T, IFlowable<U>> mapper,
                    bool delayErrors, int maxConcurrency, int bufferSize)
            {
                this.actual = actual;
                this.mapper = mapper;
                this.delayErrors = delayErrors;
                this.maxConcurrency = maxConcurrency;
                this.bufferSize = bufferSize;
                this.subscribers = EMPTY;
            }

            public void OnSubscribe(ISubscription s)
            {
                this.s = s;
                actual.OnSubscribe(this);
                if (!cancelled)
                {
                    if (maxConcurrency == int.MaxValue)
                    {
                        s.Request(long.MaxValue);
                    }
                    else
                    {
                        s.Request(maxConcurrency);
                    }
                }
            }

            public void OnNext(T t)
            {
                // safeguard against misbehaving sources
                if (done)
                {
                    return;
                }
                IFlowable <U> p;
                try
                {
                    p = mapper(t);
                }
                catch (Exception e)
                {
                    OnError(e);
                    return;
                }
                if (p is ScalarFlowable<U>) {
                    tryEmitScalar((p as ScalarFlowable<U>).value);
                } else {
                    InnerSubscriber inner = new InnerSubscriber(this, uniqueId++);
                    addInner(inner);
                    p.Subscribe(inner);
                }
            }

            void addInner(InnerSubscriber inner)
            {
                for (;;)
                {
                    InnerSubscriber[] a = subscribers;
                    if (a == CANCELLED)
                    {
                        inner.Dispose();
                        return;
                    }
                    int n = a.Length;
                    InnerSubscriber[] b = new InnerSubscriber[n + 1];
                    Array.Copy(a, 0, b, 0, n);
                    b[n] = inner;

                    if (Interlocked.CompareExchange(ref subscribers, b, a) == a)
                    {
                        return;
                    }
                }
            }

            void removeInner(InnerSubscriber inner)
            {
                for (;;)
                {
                    InnerSubscriber[] a = subscribers;
                    if (a == CANCELLED || a == EMPTY)
                    {
                        return;
                    }
                    int n = a.Length;
                    int j = -1;
                    for (int i = 0; i < n; i++)
                    {
                        if (a[i] == inner)
                        {
                            j = i;
                            break;
                        }
                    }
                    if (j < 0)
                    {
                        return;
                    }
                    InnerSubscriber[] b;
                    if (n == 1)
                    {
                        b = EMPTY;
                    }
                    else
                    {
                        b = new InnerSubscriber[n - 1];
                        
                        Array.Copy(a, 0, b, 0, j);
                        Array.Copy(a, j + 1, b, j, n - j - 1);
                    }
                    if (Interlocked.CompareExchange(ref subscribers, b, a) == a)
                    {
                        return;
                    }
                }
            }

            IQueue<U> getMainQueue()
            {
                IQueue<U> q = queue;
                if (q == null)
                {
                    if (maxConcurrency == int.MaxValue)
                    {
                        q = new SpscLinkedArrayQueue<U>(bufferSize);
                    }
                    else
                    {
                        if (QueueHelper.IsPowerOfTwo(maxConcurrency))
                        {
                            q = new SpscArrayQueue<U>(maxConcurrency);
                        }
                        else
                        {
                            q = new SpscExactArrayQueue<U>(maxConcurrency);
                        }
                    }
                    queue = q;
                }
                return q;
            }

            internal void tryEmitScalar(U value)
            {
                if (wip == 0 && Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                {
                    long r = Volatile.Read(ref requested);
                    if (r != 0L)
                    {
                        actual.OnNext(value);
                        if (r != long.MaxValue)
                        {
                            Interlocked.Decrement(ref requested);
                        }
                        if (maxConcurrency != int.MaxValue && !cancelled)
                        {
                            s.Request(1);
                        }
                    }
                    else
                    {
                        IQueue<U> q = getMainQueue();
                        if (!q.Offer(value))
                        {
                            OnError(new MissingBackpressureException());
                            return;
                        }
                    }
                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        return;
                    }
                }
                else
                {
                    IQueue<U> q = getMainQueue();
                    if (!q.Offer(value))
                    {
                        OnError(new MissingBackpressureException());
                        return;
                    }
                    if (Interlocked.Increment(ref wip) != 1)
                    {
                        return;
                    }
                }
                drainLoop();
            }

            IQueue<U> getInnerQueue(InnerSubscriber inner)
            {
                IQueue<U> q = inner.queue;
                if (q == null)
                {
                    q = new SpscArrayQueue<U>(bufferSize);
                    inner.queue = q;
                }
                return q;
            }

           internal void tryEmit(U value, InnerSubscriber inner)
            {
                if (wip == 0 && Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                {
                    long r = Volatile.Read(ref requested);
                    if (r != 0L)
                    {
                        actual.OnNext(value);
                        if (r != long.MaxValue)
                        {
                            Interlocked.Decrement(ref requested);
                        }
                        inner.requestMore(1);
                    }
                    else
                    {
                        IQueue<U> q = getInnerQueue(inner);
                        if (!q.Offer(value))
                        {
                            OnError(new MissingBackpressureException());
                            return;
                        }
                    }
                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        return;
                    }
                }
                else
                {
                    IQueue<U> q = inner.queue;
                    if (q == null)
                    {
                        q = new SpscArrayQueue<U>(bufferSize);
                        inner.queue = q;
                    }
                    if (!q.Offer(value))
                    {
                        OnError(new MissingBackpressureException());
                        return;
                    }
                    if (Interlocked.Increment(ref wip) != 1)
                    {
                        return;
                    }
                }
                drainLoop();
            }

            public void OnError(Exception t)
            {
                // safeguard against misbehaving sources
                if (done)
                {
                    return;
                }
                getErrorQueue().Enqueue(t);
                done = true;
                drain();
            }

            public void OnComplete()
            {
                // safeguard against misbehaving sources
                if (done)
                {
                    return;
                }
                done = true;
                drain();
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

            public void Cancel()
            {
                if (!cancelled)
                {
                    cancelled = true;
                    if (Interlocked.Increment(ref wip) == 1)
                    {
                        s.Cancel();
                        unsubscribe();
                    }
                }
            }

            internal void drain()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    drainLoop();
                }
            }

            void drainLoop()
            {
                ISubscriber<U> child = this.actual;
                int missed = 1;
                for (;;)
                {
                    if (checkTerminate())
                    {
                        return;
                    }
                    IQueue<U> svq = queue;

                    long r = requested;
                    bool unbounded = r == long.MaxValue;

                    long replenishMain = 0;

                    if (svq != null)
                    {
                        for (;;)
                        {
                            long scalarEmission = 0;
                            U o = null;
                            while (r != 0L)
                            {
                                o = svq.Poll();
                                if (checkTerminate())
                                {
                                    return;
                                }
                                if (o == null)
                                {
                                    break;
                                }

                                child.OnNext(o);

                                replenishMain++;
                                scalarEmission++;
                                r--;
                            }
                            if (scalarEmission != 0L)
                            {
                                if (unbounded)
                                {
                                    r = long.MaxValue;
                                }
                                else
                                {
                                    r = Interlocked.Add(ref requested, -scalarEmission);
                                }
                            }
                            if (r == 0L || o == null)
                            {
                                break;
                            }
                        }
                    }

                    bool d = done;
                    svq = queue;
                    InnerSubscriber[] inner = subscribers;
                    int n = inner.Length;

                    if (d && (svq == null || svq.IsEmpty()) && n == 0)
                    {
                        ConcurrentQueue<Exception> e = errors;
                        if (e == null || e.IsEmpty)
                        {
                            child.OnComplete();
                        }
                        else
                        {
                            reportError(e);
                        }
                        return;
                    }

                    bool innerCompleted = false;
                    if (n != 0)
                    {
                        long startId = lastId;
                        int index = lastIndex;
                        int j;
                        if (n <= index || inner[index].id != startId)
                        {
                            if (n <= index)
                            {
                                index = 0;
                            }
                            j = index;
                            for (int i = 0; i < n; i++)
                            {
                                if (inner[j].id == startId)
                                {
                                    break;
                                }
                                j++;
                                if (j == n)
                                {
                                    j = 0;
                                }
                            }
                            index = j;
                            lastIndex = j;
                            lastId = inner[j].id;
                        }

                        j = index;
                        for (int i = 0; i < n; i++)
                        {
                            if (checkTerminate())
                            {
                                return;
                            }
                            InnerSubscriber es = inner[j];

                            U o = null;
                            for (;;)
                            {
                                long produced = 0;
                                while (r != 0L)
                                {
                                    if (checkTerminate())
                                    {
                                        return;
                                    }
                                    IQueue<U> q = es.queue;
                                    if (q == null)
                                    {
                                        break;
                                    }
                                    o = q.Poll();
                                    if (o == null)
                                    {
                                        break;
                                    }

                                    child.OnNext(o);

                                    r--;
                                    produced++;
                                }
                                if (produced != 0L)
                                {
                                    if (!unbounded)
                                    {
                                        r = Interlocked.Add(ref requested, -produced);
                                    }
                                    else
                                    {
                                        r = long.MaxValue;
                                    }
                                        es.requestMore(produced);
                                }
                                if (r == 0 || o == null)
                                {
                                    break;
                                }
                            }
                            bool innerDone = es.done;
                            IQueue<U> innerQueue = es.queue;
                            if (innerDone && (innerQueue == null || innerQueue.IsEmpty()))
                            {
                                removeInner(es);
                                if (checkTerminate())
                                {
                                    return;
                                }
                                replenishMain++;
                                innerCompleted = true;
                            }
                            if (r == 0L)
                            {
                                break;
                            }

                            j++;
                            if (j == n)
                            {
                                j = 0;
                            }
                        }
                        lastIndex = j;
                        lastId = inner[j].id;
                    }

                    if (replenishMain != 0L && !cancelled)
                    {
                        s.Request(replenishMain);
                    }
                    if (innerCompleted)
                    {
                        continue;
                    }
                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            bool checkTerminate()
            {
                if (cancelled)
                {
                    s.Cancel();
                    unsubscribe();
                    return true;
                }
                ConcurrentQueue<Exception> e = errors;
                if (!delayErrors && (e != null && !e.IsEmpty))
                {
                    try
                    {
                        reportError(e);
                    }
                    finally
                    {
                        unsubscribe();
                    }
                    return true;
                }
                return false;
            }

            void reportError(ConcurrentQueue<Exception> q)
            {
                Exception ex = null;

                Exception t;
                int count = 0;
                
                while (q.TryDequeue(out t))
                {
                    if (count == 0)
                    {
                        ex = t;
                    }
                    else
                    {
                        ex.Data.Add(count, t);
                    }

                    count++;
                }
                actual.OnError(ex);
            }

            void unsubscribe()
            {
                InnerSubscriber[] a = subscribers;
                if (a != CANCELLED)
                {
                    a = Interlocked.Exchange(ref subscribers, CANCELLED);
                    if (a != CANCELLED)
                    {
                        foreach (InnerSubscriber inner in a)
                        {
                            inner.Dispose();
                        }
                    }
                }
            }

            internal ConcurrentQueue<Exception> getErrorQueue()
            {
                for (;;)
                {
                    ConcurrentQueue<Exception> q = errors;
                    if (q != null)
                    {
                        return q;
                    }
                    q = new ConcurrentQueue<Exception>();
                    if (Interlocked.CompareExchange(ref errors, q, null) == null)
                    {
                        return q;
                    }
                }
            }
        }

        sealed class InnerSubscriber : ISubscriber<U>, IDisposable {
            internal readonly long id;
            readonly MergeSubscriber parent;
            readonly int limit;
            readonly int bufferSize;

            internal volatile bool done;
            internal volatile IQueue<U> queue;
            int outstanding;

            ISubscription s;

            static readonly ISubscription CANCELLED = new BooleanSubscription();
        
            public InnerSubscriber(MergeSubscriber parent, long id)
            {
                this.id = id;
                this.parent = parent;
                this.bufferSize = parent.bufferSize;
                this.limit = bufferSize >> 2;
            }
            public void OnSubscribe(ISubscription s)
            {
                if (Interlocked.CompareExchange(ref this.s, s, null) != null)
                {
                    s.Cancel();
                    return;
                }
                outstanding = bufferSize;
                s.Request(outstanding);
            }
            public void OnNext(U t)
            {
                parent.tryEmit(t, this);
            }

            public void OnError(Exception t)
            {
                parent.getErrorQueue().Enqueue(t);
                done = true;
                parent.drain();
            }
            public void OnComplete()
            {
                done = true;
                parent.drain();
            }

            public void requestMore(long n)
            {
                int r = outstanding - (int)n;
                if (r > limit)
                {
                    outstanding = r;
                    return;
                }
                outstanding = bufferSize;
                int k = bufferSize - r;
                if (k > 0)
                {
                    s.Request(k);
                }
            }

            public void Dispose()
            {
                ISubscription a = s;
                if (a != CANCELLED)
                {
                    a = Interlocked.Exchange(ref s, CANCELLED);
                    if (a != CANCELLED && a != null)
                    {
                        a.Cancel();
                    }
                }
            }
        }
    }
}

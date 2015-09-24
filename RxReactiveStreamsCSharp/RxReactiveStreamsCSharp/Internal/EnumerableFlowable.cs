using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal
{
    sealed class EnumerableFlowable<T> : IFlowable<T>
    {
        readonly IEnumerable<T> enumerable;

        public EnumerableFlowable(IEnumerable<T> enumerable)
        {
            this.enumerable = enumerable;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            IEnumerator<T> etor = enumerable.GetEnumerator();

            var subscription = new EnumerableSubscription(s, etor);

            bool b;

            try {
                b = subscription.hasNext(etor);
            }
            catch (Exception e)
            {
                s.OnSubscribe(EmptySubscription.INSTANCE);
                s.OnError(e);
                return;
            }

            if (!b)
            {
                s.OnSubscribe(EmptySubscription.INSTANCE);
                s.OnComplete();
                return;
            }

            s.OnSubscribe(subscription);
        }

        sealed class EnumerableSubscription : ISubscription
        {

            readonly ISubscriber<T> actual;

            readonly IEnumerator<T> enumerator;

            volatile bool cancelled;

            long requested;

            public EnumerableSubscription(ISubscriber<T> actual, IEnumerator<T> enumerator)
            {
                this.actual = actual;
                this.enumerator = enumerator;
            }

            public void Cancel()
            {
                cancelled = true;
            }

            public void Request(long n)
            {
                if (n <= 0)
                {
                    return;
                }

                for (;;)
                {
                    long r = Interlocked.Read(ref requested);

                    long u = r + n;
                    if (u < 0L)
                    {
                        u = long.MaxValue;
                    }
                    if (Interlocked.CompareExchange(ref requested, u, r) == r)
                    {
                        if (r == 0L) {
                            if (u == long.MaxValue)
                            {
                                FastPath();
                            }
                            else
                            {
                                SlowPath(u);
                            }
                        }
                        break;
                    }
                }

            }

            void FastPath()
            {
                ISubscriber<T> a = actual;
                IEnumerator<T> en = enumerator;
                while (!cancelled)
                {

                    bool b;

                    try {
                        b = en.MoveNext();
                    }
                    catch (Exception e)
                    {
                        cancelled = true;
                        a.OnError(e);
                        return;
                    }

                    if (!b)
                    {
                        if (!cancelled)
                        {
                            a.OnComplete();
                        }
                        return;
                    }

                    a.OnNext(en.Current);

                    if (cancelled)
                    {
                        return;
                    }
                }

            }

            bool hasNextValue;
            bool valueConsumed;

            public bool hasNext(IEnumerator<T> en)
            {
                if (valueConsumed)
                {
                    valueConsumed = false;
                    hasNextValue = en.MoveNext();
                }
                return hasNextValue;
            }

            T next(IEnumerator<T> en)
            {
                T v = en.Current;
                valueConsumed = true;
                return v;
            }

            void SlowPath(long r)
            {
                ISubscriber<T> a = actual;
                IEnumerator<T> en = enumerator;
                for (;;)
                {
                    if (cancelled)
                    {
                        return;
                    }
                    long e = 0L;

                    while (r != 0L)
                    {
                        T v = next(en);

                        a.OnNext(v);

                        if (cancelled)
                        {
                            return;
                        }

                        bool b;
                        
                        try
                        {
                            b = hasNext(en);
                        }
                        catch (Exception ex)
                        {
                            cancelled = true;
                            a.OnError(ex);
                            return;
                        }

                        if (!b)
                        {
                            a.OnComplete();
                            return;
                        }

                        r--;
                        e--;
                    }

                    if (e != 0L)
                    {
                        r = Interlocked.Add(ref requested, e);
                    }
                    if (r == 0)
                    {
                        break;
                    }
                }
            }

        }
    }

}

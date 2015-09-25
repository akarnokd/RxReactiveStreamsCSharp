using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal
{
    sealed class RangeFlowable : IFlowable<int>
    {
        readonly long start;
        readonly long end;

        public RangeFlowable(int start, int count)
        {
            this.start = start;
            this.end = start + count - 1;
        }

        public void Subscribe(ISubscriber<int> s)
        {
            s.OnSubscribe(new RangeSubscription(this, s));
        }

        sealed class RangeSubscription : ISubscription
        {

            readonly ISubscriber<int> actual;

            volatile bool cancelled;

            long requested;

            long index;
            readonly long end;

            public RangeSubscription(RangeFlowable parent, ISubscriber<int> actual)
            {
                index = parent.start;
                end = parent.end;
                this.actual = actual;
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

                    long u = BackpressureHelper.addCap(r, n);
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
                long e = end;
                ISubscriber<int> a = actual;
                for (long i = index; i != e && !cancelled; i++)
                {
                    a.OnNext((int)i);
                }

                if (!cancelled)
                {
                    a.OnComplete();
                }
            }

            void SlowPath(long r)
            {
                long end = this.end;
                ISubscriber<int> a = actual;
                for (;;)
                {
                    if (cancelled)
                    {
                        return;
                    }
                    long e = 0L;
                    long i = index;

                    while (r != 0L && i != end)
                    {
                        a.OnNext((int)i);

                        if (cancelled)
                        {
                            return;
                        }

                        i++;
                        r--;
                        e--;
                    }

                    if (cancelled)
                    {
                        return;
                    }

                    if (i == end)
                    {
                        a.OnComplete();
                        return;
                    }

                    index = i;
                    r = Interlocked.Add(ref requested, e);
                    if (r == 0)
                    {
                        break;
                    }
                }
            }

        }
    }

}

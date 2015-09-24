using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Operators
{
    public sealed class OperatorOnBackpressureDrop<T> : IFlowableOperator<T, T>
    {
        readonly Action<T> onDrop;

        public OperatorOnBackpressureDrop(Action<T> onDrop)
        {
            this.onDrop = onDrop;
        }

        public ISubscriber<T> Invoke(ISubscriber<T> child)
        {
            return new DropSubscriber(child, onDrop);
        }

        sealed class DropSubscriber : ISubscriber<T>, ISubscription
        {
            readonly ISubscriber<T> actual;
            readonly Action<T> onDrop;

            long requested;

            ISubscription s;

            bool done;

            public DropSubscriber(ISubscriber<T> actual, Action<T> onDrop)
            {
                this.actual = actual;
                this.onDrop = onDrop;
            }

            public void Cancel()
            {
                s.Cancel();
            }

            public void OnComplete()
            {
                if (done)
                {
                    return;
                }
                done = true;
                actual.OnComplete();
            }

            public void OnError(Exception e)
            {
                if (done)
                {
                    return;
                }
                done = true;
                actual.OnError(e);
            }

            public void OnNext(T t)
            {
                if (done)
                {
                    return;
                }
                long r = Volatile.Read(ref requested);
                if (r != 0L)
                {
                    actual.OnNext(t);
                    if (r != long.MaxValue)
                    {
                        Interlocked.Add(ref requested, -1);
                    }
                } else
                {
                    try {
                        onDrop(t);
                    } catch (Exception ex)
                    {
                        done = true;
                        s.Cancel();
                        actual.OnError(ex);
                    }
                }
            }

            public void OnSubscribe(ISubscription s)
            {
                this.s = s;
                actual.OnSubscribe(this);
                s.Request(long.MaxValue);
            }

            public void Request(long n)
            {
                if (n <= 0)
                {
                    return;
                }
                BackpressureHelper.add(ref requested, n);
            }
        }
    }
}

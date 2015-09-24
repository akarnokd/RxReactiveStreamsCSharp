using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Operators
{
    public sealed class OperatorTake<T> : IFlowableOperator<T, T>
    {
        readonly long limit;

        public OperatorTake(long limit)
        {
            this.limit = limit;
        }

        public ISubscriber<T> Invoke(ISubscriber<T> child)
        {
            return new TakeSubscriber(child, limit);
        }

        sealed class TakeSubscriber : ISubscriber<T>
        {
            readonly ISubscriber<T> actual;
            readonly long limit;
            long count;

            bool done;

            public TakeSubscriber(ISubscriber<T> actual, long limit)
            {
                this.actual = actual;
                this.limit = limit;
            }

            public void OnSubscribe(ISubscription s)
            {
                actual.OnSubscribe(s);
            }

            public void OnNext(T t)
            {
                if (done)
                {
                    return;
                }

                if (count++ < limit)
                {
                    bool stop = count == limit;
                    actual.OnNext(t);
                    if (stop)
                    {
                        OnComplete();
                        return;
                    }
                }
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

            public void OnComplete()
            {
                if (done)
                {
                    return;
                }
                done = true;
                actual.OnComplete();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Operators
{
    public sealed class OperatorFilter<T> : IFlowableOperator<T, T>
    {
        readonly Func<T, bool> predicate;

        public OperatorFilter(Func<T, bool> predicate)
        {
            this.predicate = predicate;
        }

        public ISubscriber<T> Invoke(ISubscriber<T> child)
        {
            return new FilterSubscriber(child, predicate);
        }

        sealed class FilterSubscriber : ISubscriber<T>
        {
            readonly ISubscriber<T> actual;
            readonly Func<T, bool> predicate;

            ISubscription s;

            bool done;

            public FilterSubscriber(ISubscriber<T> actual, Func<T, bool> predicate)
            {
                this.actual = actual;
                this.predicate = predicate;
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

                bool b;

                try {
                    b = predicate(t);
                } catch (Exception ex)
                {
                    done = true;
                    s.Cancel();
                    actual.OnError(ex);
                    return;
                }
                if (b)
                {
                    actual.OnNext(t);
                } else
                {
                    s.Request(1);
                }
            }

            public void OnSubscribe(ISubscription s)
            {
                this.s = s;
                actual.OnSubscribe(s);
            }
        }
    }
}

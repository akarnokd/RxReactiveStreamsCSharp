using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Operators
{
    public sealed class OperatorMap<T, R> : IFlowableOperator<T, R>
    {
        readonly Func<T, R> mapper;

        public OperatorMap(Func<T, R> mapper)
        {
            this.mapper = mapper;
        }

        public ISubscriber<T> Invoke(ISubscriber<R> child)
        {
            return new MapSubscriber(child, mapper);
        }

        sealed class MapSubscriber : ISubscriber<T>
        {
            readonly ISubscriber<R> actual;
            readonly Func<T, R> mapper;

            bool done;

            ISubscription s;

            public MapSubscriber(ISubscriber<R> actual, Func<T, R> mapper)
            {
                this.actual = actual;
                this.mapper = mapper;
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
                R v;

                try {
                    v = mapper(t);
                } catch (Exception ex)
                {
                    done = true;
                    s.Cancel();
                    actual.OnError(ex);
                    return;
                }

                actual.OnNext(v);
            }

            public void OnSubscribe(ISubscription s)
            {
                this.s = s;
                actual.OnSubscribe(s);
            }
        }
    }
}

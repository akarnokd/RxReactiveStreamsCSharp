using RxReactiveStreamsCSharp.Internal.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal
{
    public sealed class FlowableObservable<T> : IObservable<T>
    {
        readonly IFlowable<T> source;

        public FlowableObservable(IFlowable<T> source)
        {
            this.source = source;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            SourceSubscriber s = new SourceSubscriber(observer);

            source.Subscribe(s);

            return s;
        }

        sealed class SourceSubscriber : ISubscriber<T>, IDisposable
        {
            readonly IObserver<T> actual;

            ISubscription s;

            static readonly ISubscription CANCELLED = new BooleanSubscription();

            public SourceSubscriber(IObserver<T> actual)
            {
                this.actual = actual;
            }

            public void Dispose()
            {
                ISubscription a = Volatile.Read(ref s);
                if (a != CANCELLED)
                {
                    a = Interlocked.Exchange(ref s, CANCELLED);
                    if (a != CANCELLED && a != null)
                    {
                        a.Cancel();
                    }
                }
            }

            public void OnComplete()
            {
                actual.OnCompleted();
            }

            public void OnError(Exception e)
            {
                actual.OnError(e);
            }

            public void OnNext(T t)
            {
                actual.OnNext(t);
            }

            public void OnSubscribe(ISubscription s)
            {
                if (Interlocked.CompareExchange(ref this.s, s, null) == null)
                {
                    s.Request(long.MaxValue);
                } else
                {
                    s.Cancel();
                }
            }


        }
    }
}

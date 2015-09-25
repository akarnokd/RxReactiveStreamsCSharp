using RxReactiveStreamsCSharp.Internal.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal
{
    public sealed class ObservableFlowable<T> : IFlowable<T>
    {
        readonly IObservable<T> source;

        public ObservableFlowable(IObservable<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            BooleanSubscription bs = new BooleanSubscription();
            s.OnSubscribe(bs);

            SourceObserver so = new SourceObserver(s);

            IDisposable d = source.Subscribe(so);

            bs.Set(d.Dispose);
        }

        sealed class SourceObserver : IObserver<T>
        {
            readonly ISubscriber<T> actual;

            public SourceObserver(ISubscriber<T> actual)
            {
                this.actual = actual;
            }

            public void OnCompleted()
            {
                actual.OnComplete();
            }

            public void OnError(Exception error)
            {
                actual.OnError(error);
            }

            public void OnNext(T value)
            {
                actual.OnNext(value);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    public sealed class Subscriber
    {
        private Subscriber()
        {
            throw new Exception("No instances!");
        }

        public static ISubscriber<T> Create<T>(Action<T> onNext)
        {
            return Create(onNext, e => { }, () => { }, s => s.Request(long.MaxValue));
        }

        public static ISubscriber<T> Create<T>(Action<T> onNext, Action<Exception> onError)
        {
            return Create(onNext, onError, () => { }, s => s.Request(long.MaxValue));
        }

        public static ISubscriber<T> Create<T>(Action<T> onNext,
            Action<Exception> onError, Action onComplete)
        {
            return Create(onNext, onError, onComplete, s => s.Request(long.MaxValue));
        }


        public static ISubscriber<T> Create<T>(Action<T> onNext, 
            Action<Exception> onError, Action onComplete, Action<ISubscription> onSubscribe)
        {
            return new LambdaSubscriber<T>(onSubscribe, onNext, onError, onComplete);
        }

        sealed class LambdaSubscriber<T> : ISubscriber<T>
        {
            readonly Action<ISubscription> onSubscribe;
            readonly Action<T> onNext;
            readonly Action<Exception> onError;
            readonly Action onComplete;

            public LambdaSubscriber(Action<ISubscription> onSubscribe, Action<T> onNext, Action<Exception> onError, Action onComplete)
            {
                this.onSubscribe = onSubscribe;
                this.onNext = onNext;
                this.onError = onError;
                this.onComplete = onComplete;
            }

            public void OnComplete()
            {
                onComplete();
            }

            public void OnError(Exception e)
            {
                onError(e);
            }

            public void OnNext(T t)
            {
                onNext(t);
            }

            public void OnSubscribe(ISubscription s)
            {
                onSubscribe(s);
            }
        }
    }
}

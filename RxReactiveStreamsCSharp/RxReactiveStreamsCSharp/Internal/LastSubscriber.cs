using System;
using System.Threading;

namespace RxReactiveStreamsCSharp.Internal
{
    public sealed class LastSubscriber<T> : ISubscriber<T>
    {
        public T value { get; private set; }

        public readonly CountdownEvent cde = new CountdownEvent(1);

        public Exception error { get; private set; }

        public void OnComplete()
        {
            cde.Signal();
        }

        public void OnError(Exception e)
        {
            error = e;
            cde.Signal();
        }

        public void OnNext(T t)
        {
            value = t;
        }

        public void OnSubscribe(ISubscription s)
        {
            s.Request(long.MaxValue);
        }
    }
}

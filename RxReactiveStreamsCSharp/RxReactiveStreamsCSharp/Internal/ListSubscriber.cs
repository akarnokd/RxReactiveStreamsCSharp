using System;
using System.Collections.Generic;
using System.Threading;

namespace RxReactiveStreamsCSharp.Internal
{
    public sealed class ListSubscriber<T> : ISubscriber<T>
    {
        public readonly List<T> values = new List<T>();

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
            values.Add(t);
        }

        public void OnSubscribe(ISubscription s)
        {
            s.Request(long.MaxValue);
        }
    }
}

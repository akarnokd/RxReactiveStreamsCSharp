using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    sealed class ScalarFlowable<T> : IFlowable<T>
    {
        readonly T _value;

        public ScalarFlowable(T value)
        {
            _value = value;
        }

        public T value { get { return _value; } }

        public void Subscribe(ISubscriber<T> s)
        {
            s.OnSubscribe(EmptySubscription.INSTANCE);
            s.OnNext(_value);
            s.OnComplete();
        }
    }
}

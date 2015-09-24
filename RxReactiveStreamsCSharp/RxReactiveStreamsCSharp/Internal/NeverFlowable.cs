using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    sealed class NeverFlowable<T> : IFlowable<T>
    {
        public void Subscribe(ISubscriber<T> s)
        {
            s.OnSubscribe(EmptySubscription.INSTANCE);
        }

    }
}

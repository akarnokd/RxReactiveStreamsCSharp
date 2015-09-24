using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal
{
    sealed class ErrorFlowable<T> : IFlowable<T>
    {
        readonly Func<Exception> errorSupplier;

        public ErrorFlowable(Func<Exception> errorSupplier)
        {
            this.errorSupplier = errorSupplier;
        }

        public void Subscribe(ISubscriber<T> s)
        {
            Exception e = errorSupplier();

            s.OnSubscribe(EmptySubscription.INSTANCE);
            s.OnError(e);
        }
    }
}

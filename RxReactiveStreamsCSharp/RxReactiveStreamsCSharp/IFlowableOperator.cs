using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    public delegate ISubscriber<T> IFlowableOperatorDelegate<T, R>(ISubscriber<R> s);

    public interface IFlowableOperator<T, R>
    {
        ISubscriber<T> Invoke(ISubscriber<R> child);
    }
}

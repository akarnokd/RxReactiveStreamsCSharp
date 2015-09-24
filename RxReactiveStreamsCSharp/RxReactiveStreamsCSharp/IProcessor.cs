using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    public interface IProcessor<in T, out R> : IFlowable<R>, ISubscriber<T>
    {

    }
}

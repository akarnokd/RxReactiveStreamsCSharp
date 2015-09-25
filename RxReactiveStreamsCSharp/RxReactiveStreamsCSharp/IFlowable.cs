using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    public interface IFlowable<out T>
    {
        void Subscribe(ISubscriber<T> s);
    }
}

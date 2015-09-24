using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    public delegate IFlowable<R> IFlowableTransformerDelegate<T, R>(IFlowable<T> source);

    public interface IFlowableTransformer<T, R>
    {
        IFlowable<R> Invoke(IFlowable<T> source);
    }
}

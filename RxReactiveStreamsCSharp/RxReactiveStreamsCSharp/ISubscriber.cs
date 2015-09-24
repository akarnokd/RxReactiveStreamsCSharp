using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    public interface ISubscriber<in T>
    {
        void OnSubscribe(ISubscription s);

        void OnNext(T t);

        void OnError(Exception e);

        void OnComplete();
    }
}

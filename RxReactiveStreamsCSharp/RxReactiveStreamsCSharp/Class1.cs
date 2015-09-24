using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    public interface IFlowable<out T>
    {

    }

    public interface ISubscriber<in T>
    {

    }

    public interface ISubscription
    {

    }

    public interface IProcessor<in T, out R>
    {

    }
}

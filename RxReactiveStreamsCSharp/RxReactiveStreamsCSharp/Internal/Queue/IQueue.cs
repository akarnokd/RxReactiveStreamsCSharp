using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Queue
{
    public interface IQueue<T> where T : class
    {
        bool Offer(T value);

        T Peek();

        T Poll();

        int Size();

        bool IsEmpty();

        void Clear();
    }
}

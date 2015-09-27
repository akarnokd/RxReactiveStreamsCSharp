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
    
    public interface IStructuredQueue<T> where T : struct
    {
        bool Offer(T value);

        bool TryPeek(out T value);

        bool TryPoll(out T value);

        int Size();

        bool IsEmpty();

        void Clear();
    }
}

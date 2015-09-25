using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Disposables
{
    public sealed class MultipleAssignmentDisposable : IDisposable
    {
        IDisposable current;

        static readonly IDisposable DISPOSED = new BooleanDisposable();

        public MultipleAssignmentDisposable()
        {

        }

        public MultipleAssignmentDisposable(IDisposable d)
        {
            current = d;
        }

        public bool Set(IDisposable next)
        {
            for (;;)
            {
                IDisposable d = Volatile.Read(ref current);
                if (d == DISPOSED)
                {
                    next.Dispose();
                    return false;
                }
                if (Interlocked.CompareExchange(ref current, next, d) == d)
                {
                    return true;
                }
            }
        }

        public void Dispose()
        {
            IDisposable d = Volatile.Read(ref current);
            if (d != DISPOSED)
            {
                d = Interlocked.Exchange(ref current, DISPOSED);
                if (d != DISPOSED && d != null)
                {
                    d.Dispose();
                }
            }
        }

        public bool IsDisposed()
        {
            return Volatile.Read(ref current) != DISPOSED;
        }
    }
}

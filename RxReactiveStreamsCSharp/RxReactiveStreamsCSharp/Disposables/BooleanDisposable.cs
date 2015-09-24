using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Disposables
{
    public sealed class BooleanDisposable : IDisposable
    {

        Action action;

        static readonly Action DISPOSED = () => { };

        public BooleanDisposable() : this(() => { })
        {
        }

        public BooleanDisposable(Action action)
        {
            this.action = action;
        }

        public void Dispose()
        {
            Action d = Volatile.Read(ref action);
            if (d != DISPOSED)
            {
                d = Interlocked.Exchange(ref action, DISPOSED);

                if (d != DISPOSED)
                {
                    d();
                }
            }
        }

        public bool IsDisposed()
        {
            return Volatile.Read(ref action) == DISPOSED;
        }
    }
}

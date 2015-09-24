using RxReactiveStreamsCSharp.Disposables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Schedulers
{
    public sealed class ScheduledRunnable : IRunnable, IDisposable
    {
        readonly IRunnable run;

        IDisposable parent;
        IDisposable future;

        static readonly IDisposable DISPOSED = new BooleanDisposable();

        static readonly IDisposable FINISHED = new BooleanDisposable();

        public ScheduledRunnable(IRunnable run, IDisposable parent)
        {
            this.run = run;
            this.parent = parent;
        }

        public void Run()
        {
            try
            {
                run.Run();
            } finally
            {
                Finish();
            }
            
        }

        void Finish()
        {
            IDisposable d = Volatile.Read(ref parent);
            if (d != DISPOSED)
            {
                if (Interlocked.CompareExchange(ref parent, FINISHED, d) == d)
                {
                    if (d != null)
                    {
                        ((ICompositeDisposable)d).Delete(this);
                    }
                }
            }

            for (;;)
            {
                d = Volatile.Read(ref future);

                if (d == DISPOSED)
                {
                    break;
                }

                if (Interlocked.CompareExchange(ref future, FINISHED, d) == d)
                {
                    break;
                }
            }
        }

        public void SetFuture(IDisposable f)
        {
            for (;;)
            {
                IDisposable d = Volatile.Read(ref future);
                if (d == FINISHED)
                {
                    break;
                }
                if (d == DISPOSED)
                {
                    f.Dispose();
                    break;
                }
                if (d != null)
                {
                    throw new Exception("Future already set!");
                }
                if (Interlocked.CompareExchange(ref future, f, null) == null)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            for (;;)
            {
                IDisposable d = Volatile.Read(ref future);
                if (d == DISPOSED || d == FINISHED)
                {
                    break;
                }
                if (Interlocked.CompareExchange(ref future, DISPOSED, d) == d)
                {
                    if (d != null)
                    {
                        d.Dispose();
                    }
                    break;
                }
            }
            for (;;)
            {
                IDisposable d = Volatile.Read(ref parent);
                if (d == DISPOSED || d == FINISHED)
                {
                    break;
                }

                if (Interlocked.CompareExchange(ref parent, DISPOSED, d) == d)
                {
                    if (d != null)
                    {
                        ((ICompositeDisposable)d).Delete(this);
                    }
                    break;
                }
            }
        }

        public bool IsDisposed()
        {
            return Volatile.Read(ref future) == DISPOSED;
        }
    }
}

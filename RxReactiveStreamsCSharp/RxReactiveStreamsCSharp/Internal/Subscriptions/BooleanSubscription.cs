using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Subscriptions
{
    public sealed class BooleanSubscription : ISubscription, IDisposable
    {
        Action onCancel;

        static readonly Action CANCELLED = () => { };

        public BooleanSubscription()
        {

        }

        public BooleanSubscription(Action onCancel)
        {
            this.onCancel = onCancel;
        }

        public void Cancel()
        {
            Action a = Volatile.Read(ref onCancel);
            if (a != CANCELLED)
            {
                a = Interlocked.Exchange(ref onCancel, CANCELLED);
                if (a != CANCELLED && a != null)
                {
                    a();
                }
            }
        }

        public bool Set(Action action)
        {
            for (;;)
            {
                Action a = Volatile.Read(ref onCancel);
                if (a == CANCELLED)
                {
                    action();
                    return false;
                }
                if (Interlocked.CompareExchange(ref onCancel, action, a) == a)
                {
                    if (a != null)
                    {
                        a();
                    }
                    return true;
                }
            }
        }

        public bool Replace(Action action)
        {
            for (;;)
            {
                Action a = Volatile.Read(ref onCancel);
                if (a == CANCELLED)
                {
                    action();
                    return false;
                }
                if (Interlocked.CompareExchange(ref onCancel, action, a) == a)
                {
                    return true;
                }
            }
        }

        public void Request(long n)
        {
            // ignored
        }

        public bool IsDisposed()
        {
            return IsCancelled();
        }

        public bool IsCancelled()
        {
            return Volatile.Read(ref onCancel) == CANCELLED;
        }

        public void Dispose()
        {
            Cancel();
        }
    }
}

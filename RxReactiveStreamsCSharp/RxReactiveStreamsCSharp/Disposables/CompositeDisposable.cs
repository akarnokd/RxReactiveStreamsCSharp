using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Disposables
{
    public sealed class SetCompositeDisposable : IDisposable
    {
        volatile bool disposed;

        HashSet<IDisposable> set;

        public bool Add(IDisposable d)
        {
            if (disposed)
            {
                d.Dispose();
                return false;
            }

            lock (this)
            {
                if (!disposed)
                {
                    var set = this.set;
                    if (set == null)
                    {
                        set = new HashSet<IDisposable>();
                        this.set = set;
                    }

                    set.Add(d);

                    return true;
                }
            }

            d.Dispose();
            return false;
        }

        public bool Delete(IDisposable d)
        {
            if (disposed)
            {
                return false;
            }
            lock (this)
            {
                if (disposed)
                {
                    return false;
                }

                HashSet<IDisposable> set = this.set;

                return set != null && set.Remove(d);
            }
        }

        public void Remove(IDisposable d)
        {
            if (Delete(d))
            {
                d.Dispose();
            }
        }

        public void Clear()
        {
            if (disposed)
            {
                return;
            }

            HashSet<IDisposable> set;
            lock (this)
            {
                if (disposed)
                {
                    return;
                }

                set = this.set;
                this.set = null;
            }

            if (set != null)
            {
                foreach (IDisposable d in set)
                {
                    d.Dispose();
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            HashSet<IDisposable> set;
            lock (this)
            {
                if (disposed)
                {
                    return;
                }
                disposed = true;

                set = this.set;
                this.set = null;
            }

            if (set != null)
            {
                foreach (IDisposable d in set)
                {
                    d.Dispose();
                }
            }
        }

        public bool IsDisposed()
        {
            return disposed;
        }
    }

    public sealed class ListCompositeDisposable : IDisposable
    {
        volatile bool disposed;

        LinkedList<IDisposable> set;

        public bool Add(IDisposable d)
        {
            if (disposed)
            {
                d.Dispose();
                return false;
            }

            lock (this)
            {
                if (!disposed)
                {
                    var set = this.set;
                    if (set == null)
                    {
                        set = new LinkedList<IDisposable>();
                        this.set = set;
                    }

                    set.AddLast(d);

                    return true;
                }
            }

            d.Dispose();
            return false;
        }

        public bool Delete(IDisposable d)
        {
            if (disposed)
            {
                return false;
            }
            lock (this)
            {
                if (disposed)
                {
                    return false;
                }

                LinkedList<IDisposable> set = this.set;

                return set != null && set.Remove(d);
            }
        }

        public void Remove(IDisposable d)
        {
            if (Delete(d))
            {
                d.Dispose();
            }
        }

        public void Clear()
        {
            if (disposed)
            {
                return;
            }

            LinkedList<IDisposable> set;
            lock (this)
            {
                if (disposed)
                {
                    return;
                }

                set = this.set;
                this.set = null;
            }

            if (set != null)
            {
                foreach (IDisposable d in set)
                {
                    d.Dispose();
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            LinkedList<IDisposable> set;
            lock (this)
            {
                if (disposed)
                {
                    return;
                }
                disposed = true;

                set = this.set;
                this.set = null;
            }

            if (set != null)
            {
                foreach (IDisposable d in set)
                {
                    d.Dispose();
                }
            }
        }

        public bool isDisposed()
        {
            return disposed;
        }
    }

}

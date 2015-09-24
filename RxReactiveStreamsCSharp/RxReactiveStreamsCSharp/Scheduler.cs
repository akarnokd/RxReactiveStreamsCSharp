using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    public abstract class Scheduler
    {
        public IDisposable ScheduleDirect(Action action)
        {
            return ScheduleDirect(action, TimeSpan.Zero);
        }

        public IDisposable ScheduleDirect(Action action, TimeSpan delay)
        {
            Worker w = CreateWorker();

            w.Schedule(() =>
            {
                try
                {
                    action();
                }
                finally
                {
                    w.Dispose();
                }
            }, delay);

            return w;
        }

        public IDisposable SchedulePeriodicallyDirect(Action action, TimeSpan initialDelay, TimeSpan period)
        {
            Worker w = CreateWorker();

            w.SchedulePeriodically(() =>
            {
                try
                {
                    action();
                }
                catch
                {
                    w.Dispose();
                }
            }, initialDelay, period);

            return w;
        }

        public abstract Worker CreateWorker();

        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public long now()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }

        public abstract class Worker : IDisposable
        {

            public IDisposable Schedule(Action action)
            {
                return Schedule(action, TimeSpan.Zero);
            }

            public abstract IDisposable Schedule(Action action, TimeSpan delay);

            public IDisposable SchedulePeriodically(Action action, TimeSpan initialDelay, TimeSpan period)
            {
                throw new NotImplementedException();
            }

            public long now()
            {
                return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
            }

            public abstract void Dispose();
        }

    }
}

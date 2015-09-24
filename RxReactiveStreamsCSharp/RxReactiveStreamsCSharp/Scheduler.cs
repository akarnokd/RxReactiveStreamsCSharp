using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RxReactiveStreamsCSharp.Disposables;

namespace RxReactiveStreamsCSharp
{
    public abstract class Scheduler
    {
        public virtual IDisposable ScheduleDirect(IRunnable action)
        {
            return ScheduleDirect(action, TimeSpan.Zero);
        }

        public virtual IDisposable ScheduleDirect(Action action)
        {
            return ScheduleDirect(new ActionRunnable(action));
        }

        public virtual IDisposable ScheduleDirect(IRunnable action, TimeSpan delay)
        {
            Worker w = CreateWorker();

            w.Schedule(new DirectRunnable(w, action), delay);

            return w;
        }

        public virtual IDisposable ScheduleDirect(Action action, TimeSpan delay) {
            return ScheduleDirect(new ActionRunnable(action), delay);
        }           

        public virtual IDisposable SchedulePeriodicallyDirect(IRunnable action, TimeSpan initialDelay, TimeSpan period)
        {
            Worker w = CreateWorker();

            w.SchedulePeriodically(new DirectPeriodicRunnable(w, action), initialDelay, period);

            return w;
        }

        public virtual IDisposable SchedulePeriodicallyDirect(Action action, TimeSpan initialDelay, TimeSpan period)
        {
            return SchedulePeriodicallyDirect(new ActionRunnable(action), initialDelay, period);
        }

        public abstract Worker CreateWorker();

        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public virtual long now()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }

        public abstract class Worker : IDisposable
        {

            public virtual IDisposable Schedule(Action action)
            {
                return Schedule(new ActionRunnable(action));
            }

            public virtual IDisposable Schedule(IRunnable action)
            {
                return Schedule(action, TimeSpan.Zero);
            }

            public virtual IDisposable Schedule(Action action, TimeSpan delay)
            {
                return Schedule(new ActionRunnable(action), delay);
            }

            public abstract IDisposable Schedule(IRunnable action, TimeSpan delay);

            public virtual IDisposable SchedulePeriodically(Action action, TimeSpan initialDelay, TimeSpan period)
            {
                return SchedulePeriodically(new ActionRunnable(action), initialDelay, period);
            }

            public virtual IDisposable SchedulePeriodically(IRunnable action, TimeSpan initialDelay, TimeSpan period)
            {
                MultipleAssignmentDisposable first = new MultipleAssignmentDisposable();

                MultipleAssignmentDisposable mad = new MultipleAssignmentDisposable(first);

                IRunnable decoratedRun = action;

                first.Set(Schedule(new PeriodicRunnable(this, decoratedRun, initialDelay, period, mad), initialDelay));
            
                return mad;
            }

            public virtual long Now()
            {
                return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
            }

            public abstract void Dispose();
        }


        sealed class DirectRunnable : IRunnable
        {
            readonly Worker w;
            readonly IRunnable run;

            public DirectRunnable(Worker w, IRunnable run)
            {
                this.w = w;
                this.run = run;
            }


            public void Run()
            {
                try
                {
                    run.Run();
                } 
                finally
                {
                    w.Dispose();
                }
            }
        }

        sealed class DirectPeriodicRunnable : IRunnable
        {
            readonly Worker w;
            readonly IRunnable run;

            public DirectPeriodicRunnable(Worker w, IRunnable run)
            {
                this.w = w;
                this.run = run;
            }


            public void Run()
            {
                try
                {
                    run.Run();
                }
                catch (Exception ex)
                {
                    w.Dispose();

                    throw ex;
                }
            }
        }

        sealed class ActionRunnable : IRunnable
        {
            readonly Action action;

            public ActionRunnable(Action action)
            {
                this.action = action;
            }

            public void Run()
            {
                action();
            }
        }

        sealed class PeriodicRunnable : IRunnable
        {
            long lastNow;
            long startTime;
            long count;
            readonly IRunnable run;
            readonly Worker w;
            readonly MultipleAssignmentDisposable mad;
            readonly TimeSpan period;


            public PeriodicRunnable(Worker w, IRunnable run, TimeSpan initialDelay, TimeSpan period, MultipleAssignmentDisposable mad)
            {
                this.w = w;
                this.run = run;
                this.mad = mad;
                lastNow = w.Now();
                startTime = lastNow + initialDelay.Milliseconds;
            }

            public void Run()
            {
                run.Run();

                long t = w.Now();
                long c = ++count;

                long targetTime = startTime + c * period.Milliseconds;

                long delay;
                // if the current time is less than last time
                // avoid scheduling the next run too far in the future
                if (t < lastNow)
                {
                    delay = period.Milliseconds;
                    // TODO not sure about this correction
                    startTime -= lastNow - c * period.Milliseconds;
                }
                // if the current time is ahead of the target time, 
                // avoid scheduling a bunch of 0 delayed tasks
                else if (t > targetTime)
                {
                    delay = period.Milliseconds;
                    // TODO not sure about this correction
                    startTime += t - c * period.Milliseconds;
                }
                else
                {
                    delay = targetTime - t;
                }

                lastNow = t;

                mad.Set(w.Schedule(this, TimeSpan.FromMilliseconds(delay)));
            }
        }
    }
}

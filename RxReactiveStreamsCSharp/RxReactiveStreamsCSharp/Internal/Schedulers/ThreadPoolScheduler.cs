using RxReactiveStreamsCSharp.Disposables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal.Schedulers
{
    public sealed class ThreadPoolScheduler : Scheduler
    {

        public override IDisposable ScheduleDirect(IRunnable run)
        {
            ScheduledRunnable sr = new ScheduledRunnable(run, null);

            IDisposable d = Task.Run(() => sr.Run());
            sr.SetFuture(d);

            return sr;
        }

        public override IDisposable ScheduleDirect(IRunnable run, TimeSpan delay)
        {
            ScheduledRunnable sr = new ScheduledRunnable(run, null);

            IDisposable d = Task.Delay(delay).ContinueWith(t => sr.Run());
            sr.SetFuture(d);

            return sr;
        }

        public override Worker CreateWorker()
        {
            return new ThreadPoolWorker();
        }

        sealed class ThreadPoolWorker : Worker
        {

            readonly SetCompositeDisposable set;
            readonly ConcurrentQueue<ScheduledRunnable> queue;

            int wip;

            public ThreadPoolWorker()
            {
                set = new SetCompositeDisposable();
                queue = new ConcurrentQueue<ScheduledRunnable>();
            }

            public override void Dispose()
            {
                set.Dispose();
                if (Interlocked.Increment(ref wip) == 1)
                {
                    ScheduledRunnable v;
                    while (queue.TryDequeue(out v)) ;
                }
            }

            public override IDisposable Schedule(IRunnable action)
            {
                ScheduledRunnable sr = new ScheduledRunnable(action, set);
                set.Add(sr);

                queue.Enqueue(sr);

                if (Interlocked.Increment(ref wip) == 1)
                {
                    Task.Run(() => drain());
                }

                return sr;
            }

            public override IDisposable Schedule(IRunnable action, TimeSpan delay)
            {

                MultipleAssignmentDisposable first = new MultipleAssignmentDisposable();
                MultipleAssignmentDisposable mad = new MultipleAssignmentDisposable(first);

                IDisposable d = Task.Delay(delay).ContinueWith(t => {
                    IDisposable d1 = Schedule(action);
                    mad.Set(d1);
                });

                first.Set(d);

                return mad;
            }

            void drain()
            {
                int missed = 1;

                for (;;)
                {

                    for (;;)
                    {
                        ScheduledRunnable sr;

                        if (queue.TryDequeue(out sr))
                        {
                            if (!sr.IsDisposed())
                            {
                                sr.Run();
                            }
                        } else
                        {
                            break;
                        }

                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }
        }
    }

}

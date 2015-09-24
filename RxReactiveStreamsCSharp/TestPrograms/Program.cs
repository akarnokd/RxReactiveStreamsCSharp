using RxReactiveStreamsCSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestPrograms
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 1; i <= 1000000; i *= 1000)
            {
                // --------------------------------
                // Synchronous range
                // IObservable<int> o = Observable.Range(1, i);
                // IObserver<int> ob = Observer.Create<int>(v => { });
                // Asynchronous range
                // IObservable<int> o = Observable.Range(1, i).ObserveOn(Scheduler.Default);
                // Pipelined range
                IFlowable<int> o = Flowable.Range(1, i);
                ISubscriber<int> ob = Subscriber.Create<int>(v => { });
                // --------------------------------
                long total = 0;
                long totalElapsed = 0L;
                for (int j = 0; j < 10; j++)
                {
                    Stopwatch sw = new Stopwatch();
                    long count = 0;
                    sw.Start();
                    while (sw.ElapsedMilliseconds < 1000)
                    {
                        // --------------------------------

                        o.Subscribe(ob);

                        // --------------------------------
                        count++;
                    }
                    sw.Stop();

                    long elapsed = sw.ElapsedMilliseconds;

                    Console.WriteLine("  {0} / {1}: {2}", i, j, count * 1000d / elapsed);

                    totalElapsed += elapsed;
                    total += count;
                }
                Console.WriteLine();
                Console.WriteLine("{0} average: {1} ops/s", i, total * 1000d / totalElapsed);
            }

            Console.Read();
        }
    }
}

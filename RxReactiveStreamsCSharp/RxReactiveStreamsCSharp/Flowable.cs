using RxReactiveStreamsCSharp.Internal;
using RxReactiveStreamsCSharp.Internal.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    public static class Flowable
    {

        static T RequireNonNull<T>(T value)
        {
            if (value == null)
            {
                throw new NullReferenceException();
            }
            return value;
        }

        sealed class FlowableImpl<T> : IFlowable<T>
        {
            readonly Action<ISubscriber<T>> onSubscribe;

            public FlowableImpl(Action<ISubscriber<T>> onSubscribe)
            {
                this.onSubscribe = onSubscribe;
            }

            public void Subscribe(ISubscriber<T> s)
            {
                onSubscribe.Invoke(s);
            }
        }

        public static IFlowable<T> Create<T>(Action<ISubscriber<T>> onSubscribe)
        {
            RequireNonNull(onSubscribe);
            return new FlowableImpl<T>(onSubscribe);
        }

        public static IFlowable<T> Just<T>(T value)
        {
            RequireNonNull(value);
            return new ScalarFlowable<T>(value);
        }

        public static IFlowable<T> Never<T>()
        {
            return new NeverFlowable<T>();
        }

        public static IFlowable<T> Empty<T>()
        {
            return new EmptyFlowable<T>();
        }

        public static IFlowable<T> Error<T>(Exception e)
        {
            return Error<T>(() => e);
        }
        
        public static IFlowable<T> Error<T>(Func<Exception> errorSupplier)
        {
            return new ErrorFlowable<T>(errorSupplier);
        }

        public static T Get<T>(this IFlowable<T> flowable)
        {
            LastSubscriber<T> ls = new LastSubscriber<T>();

            flowable.Subscribe(ls);

            ls.cde.Wait();

            if (ls.error != null)
            {
                throw ls.error;
            }

            return ls.value;
        }

        public static List<T> GetList<T>(this IFlowable<T> flowable)
        {
            ListSubscriber<T> ls = new ListSubscriber<T>();

            flowable.Subscribe(ls);

            ls.cde.Wait();

            if (ls.error != null)
            {
                throw ls.error;
            }

            return ls.values;
        }

        public static IFlowable<int> Range(int start, int count)
        {
            if (count < 0)
            {
                throw new ArgumentException("count >= 0 required");
            }
            if ((long)start - 1 + count > long.MaxValue)
            {
                throw new ArithmeticException("Range would overflow");
            }

            if (count == 0)
            {
                return Empty<int>();
            }
            if (count == 1)
            {
                return Just(start);
            }
            return new RangeFlowable(start, count);
        }

        public static IFlowable<T> From<T>(IEnumerable<T> source)
        {
            return new EnumerableFlowable<T>(source);
        }

        public static IFlowable<T> ToFlowable<T>(this IEnumerable<T> source)
        {
            return From(source);
        }

        public static IFlowable<R> To<T, R>(this IFlowable<T> source, IFlowableTransformerDelegate<T, R> transformer)
        {
            return transformer(source);
        }

        public static IFlowable<R> To<T, R>(this IFlowable<T> source, IFlowableTransformer<T, R> transformer)
        {
            return transformer.Invoke(source);
        }

        public static R To<T, R>(this IFlowable<T> source, Func<IFlowable<T>, R> transformer)
        {
            return transformer(source);
        }

        public static IFlowable<R> Lift<T, R>(this IFlowable<T> source, IFlowableOperatorDelegate<T, R> lifter)
        {
            return new LiftFlowableDeleage<T, R>(source, lifter);
        }

        public static IFlowable<R> Lift<T, R>(this IFlowable<T> source, IFlowableOperator<T, R> lifter)
        {
            return new LiftFlowable<T, R>(source, lifter);
        }

        public static IFlowable<R> Map<T, R>(this IFlowable<T> source, Func<T, R> mapper)
        {
            return source.Lift(new OperatorMap<T, R>(mapper));
        }

        public static IFlowable<T> Filter<T>(this IFlowable<T> source, Func<T, bool> predicate)
        {
            return source.Lift(new OperatorFilter<T>(predicate));
        }

        public static IFlowable<T> Take<T>(this IFlowable<T> source, long limit)
        {
            return source.Lift(new OperatorTake<T>(limit));
        }

        public static int BufferSize()
        {
            return 128;
        }

        public static IFlowable<T> OnBackpressureDrop<T>(this IFlowable<T> source)
        {
            return source.OnBackpressureDrop(v => { });
        }

        public static IFlowable<T> OnBackpressureDrop<T>(this IFlowable<T> source, Action<T> onDrop)
        {
            return source.Lift(new OperatorOnBackpressureDrop<T>(onDrop));
        }

        public static IFlowable<T> OnBackpressureBuffer<T>(this IFlowable<T> source) where T : class
        {
            return source.Lift(new OperatorOnBackpressureBuffer<T>(BufferSize(), true, () => { }));
        }

        public static IFlowable<T> OnBackpressureBuffer<T>(this IFlowable<T> source, int maxCapacity) where T : class
        {
            return source.Lift(new OperatorOnBackpressureBuffer<T>(maxCapacity, false, () => { }));
        }

        public static IFlowable<T> OnBackpressureBuffer<T>(this IFlowable<T> source, int maxCapacity, Action onOverflow) where T : class
        {
            return source.Lift(new OperatorOnBackpressureBuffer<T>(maxCapacity, false, onOverflow));
        }

        public static IFlowable<T> OnBackpressureBuffer<T>(this IFlowable<T> source, int capacityHint, bool unbounded, Action onOverflow) where T : class
        {
            return source.Lift(new OperatorOnBackpressureBuffer<T>(capacityHint, unbounded, onOverflow));
        }

        public static IFlowable<T> ToFlowable<T>(this IObservable<T> source)
        {
            return new ObservableFlowable<T>(source);
        }

        public static IFlowable<T> OnBackpressureDrop<T>(this IObservable<T> source)
        {
            return source.ToFlowable().OnBackpressureDrop();
        }

        public static IFlowable<T> OnBackpressureBuffer<T>(this IObservable<T> source) where T : class
        {
            return source.ToFlowable().OnBackpressureBuffer();
        }

        public static IObservable<T> ToObservable<T>(this IFlowable<T> source)
        {
            return new FlowableObservable<T>(source);
        }

        public static IFlowable<U> FlatMap<T, U>(this IFlowable<T> source, Func<T, IFlowable<U>> mapper, bool delayError, int maxConcurrency, int bufferSize) where U : class
        {
            return source.Lift(new OperatorFlatMap<T, U>(mapper, delayError, maxConcurrency, bufferSize));
        }

        public static IFlowable<U> FlatMap<T, U>(this IFlowable<T> source, Func<T, IFlowable<U>> mapper) where U : class
        {
            return source.FlatMap(mapper, false, int.MaxValue, BufferSize());
        }

        public static IFlowable<U> FlatMap<T, U>(this IFlowable<T> source, Func<T, IFlowable<U>> mapper, bool delayError) where U : class
        {
            return source.FlatMap(mapper, delayError, int.MaxValue, BufferSize());
        }

        public static IFlowable<U> FlatMap<T, U>(this IFlowable<T> source, Func<T, IFlowable<U>> mapper, int maxConcurrency) where U : class
        {
            return source.FlatMap(mapper, false, maxConcurrency, BufferSize());
        }

        public static IFlowable<U> FlatMap<T, U>(this IFlowable<T> source, Func<T, IFlowable<U>> mapper, bool delayError, int maxConcurrency) where U : class
        {
            return source.FlatMap(mapper, delayError, maxConcurrency, BufferSize());
        }

        public static IFlowable<T> Merge<T>(this IEnumerable<IFlowable<T>> sources) where T : class
        {
            return sources.ToFlowable().FlatMap(v => v);
        }

        public static IFlowable<T> Merge<T>(this IEnumerable<IFlowable<T>> sources, bool delayError) where T : class
        {
            return sources.ToFlowable().FlatMap(v => v, delayError);
        }

        public static IFlowable<T> Merge<T>(params IFlowable<T>[] sources) where T : class
        {
            if (sources.Length == 0)
            {
                return Empty<T>();
            }
            if (sources.Length == 1)
            {
                return sources[0];
            }
            return Merge(sources);
        }

        public static IFlowable<T> MergeDelayError<T>(params IFlowable<T>[] sources) where T : class
        {
            if (sources.Length == 0)
            {
                return Empty<T>();
            }
            if (sources.Length == 1)
            {
                return sources[0];
            }
            return Merge(sources, true);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp.Internal
{
    public sealed class LiftFlowable<T, R> : IFlowable<R>
    {
        public IFlowable<T> source { get; private set; }

        public IFlowableOperator<T, R> flowOperator { get; private set; }

        public LiftFlowable(IFlowable<T> source, IFlowableOperator<T, R> flowOperator)
        {
            this.source = source;
            this.flowOperator = flowOperator;
        }

        public void Subscribe(ISubscriber<R> rs)
        {
            var ts = flowOperator.Invoke(rs);

            source.Subscribe(ts);
        }

    }

    public sealed class LiftFlowableDeleage<T, R> : IFlowable<R>
    {
        public IFlowable<T> source { get; private set; }

        public IFlowableOperatorDelegate<T, R> flowOperator { get; private set; }

        public LiftFlowableDeleage(IFlowable<T> source, IFlowableOperatorDelegate<T, R> flowOperator)
        {
            this.source = source;
            this.flowOperator = flowOperator;
        }

        public void Subscribe(ISubscriber<R> rs)
        {
            var ts = flowOperator(rs);

            source.Subscribe(ts);
        }

    }
}

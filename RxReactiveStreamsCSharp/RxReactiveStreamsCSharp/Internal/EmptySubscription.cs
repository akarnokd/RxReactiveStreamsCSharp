using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxReactiveStreamsCSharp
{
    public sealed class EmptySubscription : ISubscription
    {
        private EmptySubscription()
        {
            
        }

        public static readonly EmptySubscription INSTANCE = new EmptySubscription();

        public void Request(long n)
        {
            // ignored
        }

        public void Cancel()
        {
            // ignored
        }
    }
}

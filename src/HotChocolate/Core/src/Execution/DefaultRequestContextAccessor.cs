using System;
using System.Threading;

namespace HotChocolate.Execution
{
    internal sealed class DefaultRequestContextAccessor : IRequestContextAccessor
    {
        private static AsyncLocal<RequestContextHolder> _requestContextCurrent =
            new AsyncLocal<RequestContextHolder>();

        public IRequestContext RequestContext
        {
            get
            {
                return _requestContextCurrent.Value?.Context ??
                       throw new InvalidCastException("Can only be accessed in a request context.");
            }
            set
            {
                RequestContextHolder holder = _requestContextCurrent.Value;

                if (holder != null)
                {
                    holder.Context = null!;
                }

                if (value is not null!)
                {
                    _requestContextCurrent.Value = new RequestContextHolder
                    {
                        Context = value
                    };
                }
            }
        }

        private class RequestContextHolder
        {
            public IRequestContext Context;
        }
    }
}

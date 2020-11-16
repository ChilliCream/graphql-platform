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
                RequestContextHolder? holder = _requestContextCurrent.Value;

                if (holder is null)
                {
                    holder = new RequestContextHolder();
                    _requestContextCurrent.Value = holder;
                }

                holder.Context = value;
            }
        }

        private class RequestContextHolder
        {
            public IRequestContext Context = default!;
        }
    }
}

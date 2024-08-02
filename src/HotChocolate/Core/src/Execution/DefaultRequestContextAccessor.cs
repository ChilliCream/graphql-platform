namespace HotChocolate.Execution;

internal sealed class DefaultRequestContextAccessor : IRequestContextAccessor
{
    private static readonly AsyncLocal<RequestContextHolder> _requestContextCurrent = new();

    public IRequestContext RequestContext
    {
        get
        {
            return _requestContextCurrent.Value?.Context ??
                throw new InvalidCastException("Can only be accessed in a request context.");
        }
        set
        {
            var holder = _requestContextCurrent.Value;

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

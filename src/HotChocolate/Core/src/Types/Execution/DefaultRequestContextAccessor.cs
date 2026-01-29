namespace HotChocolate.Execution;

internal sealed class DefaultRequestContextAccessor : IRequestContextAccessor
{
    private static readonly AsyncLocal<RequestContextHolder> s_requestContextCurrent = new();

    public RequestContext RequestContext
    {
        get
        {
            return s_requestContextCurrent.Value?.Context ??
                throw new InvalidCastException("Can only be accessed in a request context.");
        }
        set
        {
            var holder = s_requestContextCurrent.Value;

            if (holder is null)
            {
                holder = new RequestContextHolder();
                s_requestContextCurrent.Value = holder;
            }

            holder.Context = value;
        }
    }

    private class RequestContextHolder
    {
        public RequestContext Context = null!;
    }
}

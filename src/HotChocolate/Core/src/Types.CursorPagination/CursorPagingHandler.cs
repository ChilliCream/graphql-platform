using System.Threading.Tasks;
using HotChocolate.Utilities;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination
{
    public abstract class CursorPagingHandler : IPagingHandler
    {
        protected CursorPagingHandler(PagingOptions options)
        {
            DefaultPageSize = options.DefaultPageSize ?? PagingDefaults.DefaultPageSize;
            MaxPageSize = options.MaxPageSize ?? PagingDefaults.MaxPageSize;

            if (MaxPageSize < DefaultPageSize)
            {
                DefaultPageSize = MaxPageSize;
            }
        }

        protected int DefaultPageSize { get; }

        protected int MaxPageSize { get; }

        public void ValidateContext(IResolverContext context)
        {
            int? first = context.ArgumentValue<int?>(CursorPagingArgumentNames.First);
            int? last = context.ArgumentValue<int?>(CursorPagingArgumentNames.Last);

            if (first > MaxPageSize || last > MaxPageSize)
            {
                throw ThrowHelper.ConnectionMiddleware_MaxPageSize();
            }
        }

        async ValueTask<IPage> IPagingHandler.SliceAsync(
            IResolverContext context,
            object source)
        {
            CursorPagingArguments arguments = context.GetPagingArguments(DefaultPageSize);

            return await SliceAsync(context, source, arguments).ConfigureAwait(false);
        }

        protected abstract ValueTask<Connection> SliceAsync(
            IResolverContext context,
            object source,
            CursorPagingArguments arguments);
    }
}

using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Pagination
{
    public abstract class CursorPagingHandler : IPagingHandler
    {
        protected CursorPagingHandler(PagingSettings settings)
        {
            DefaultPageSize = settings.DefaultPageSize ?? PagingDefaults.DefaultPageSize;
            MaxPageSize = settings.MaxPageSize ?? PagingDefaults.MaxPageSize;
            IncludeTotalCount = settings.IncludeTotalCount ?? PagingDefaults.IncludeTotalCount;
        }

        protected int DefaultPageSize { get; }

        protected  int MaxPageSize { get; }

        protected  bool IncludeTotalCount { get; }

        public void ValidateContext(IResolverContext context)
        {
            int? first = context.ArgumentValue<int?>(CursorPagingArgumentNames.First);
            int? last = context.ArgumentValue<int?>(CursorPagingArgumentNames.Last);

            if (first is null && last is null)
            {
                first = DefaultPageSize;
            }

            if (first > MaxPageSize || last > MaxPageSize)
            {
                throw ThrowHelper.ConnectionMiddleware_MaxPageSize();
            }
        }

        async ValueTask<IPage> IPagingHandler.SliceAsync(
            IResolverContext context,
            object source)
        {
            var arguments = new CursorPagingArguments(
                context.ArgumentValue<int?>(CursorPagingArgumentNames.First),
                context.ArgumentValue<int?>(CursorPagingArgumentNames.Last),
                context.ArgumentValue<string>(CursorPagingArgumentNames.After),
                context.ArgumentValue<string>(CursorPagingArgumentNames.Before));

            return await SliceAsync(context, source, arguments).ConfigureAwait(false);
        }

        protected abstract ValueTask<Connection> SliceAsync(
            IResolverContext context,
            object source,
            CursorPagingArguments arguments);
    }
}

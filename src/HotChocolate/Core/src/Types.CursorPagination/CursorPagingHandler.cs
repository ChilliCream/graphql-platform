using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
using static HotChocolate.Types.Pagination.CursorPagingArgumentNames;

namespace HotChocolate.Types.Pagination
{
    public abstract class CursorPagingHandler : IPagingHandler
    {
        private readonly PagingOptions _options;

        protected CursorPagingHandler(PagingOptions options)
        {
            _options = new PagingOptions
            {
                DefaultPageSize = options.DefaultPageSize ?? PagingDefaults.DefaultPageSize,
                MaxPageSize = options.MaxPageSize ?? PagingDefaults.MaxPageSize,
                IncludeTotalCount =
                    options.IncludeTotalCount ?? PagingDefaults.IncludeTotalCount
            };

            if (MaxPageSize < DefaultPageSize)
            {
                _options.DefaultPageSize = MaxPageSize;
            }
        }

        /// <summary>
        /// The default page size configured for this handler.
        /// </summary>
        protected int DefaultPageSize =>
            _options.DefaultPageSize ?? PagingDefaults.DefaultPageSize;

        /// <summary>
        /// The maximum allowed page size configured for this handler.
        /// </summary>
        /// <value></value>
        protected int MaxPageSize =>
            _options.MaxPageSize ?? PagingDefaults.MaxPageSize;

        /// <summary>
        /// Result should include total count.
        /// </summary>
        protected bool IncludeTotalCount =>
            _options.IncludeTotalCount ?? PagingDefaults.IncludeTotalCount;

        public void ValidateContext(IResolverContext context)
        {
            int? first = context.ArgumentValue<int?>(First);
            int? last = context.ArgumentValue<int?>(Last);

            if (first > MaxPageSize || last > MaxPageSize)
            {
                throw ThrowHelper.ConnectionMiddleware_MaxPageSize();
            }
        }

        public IExecutable ApplyExecutable(IResolverContext context, IExecutable executable)
        {
            if (executable is ICursorPagingExecutable cursorPagingExecutable)
            {
                CursorPagingArguments arguments = CreatePagingArguments(context);
                return cursorPagingExecutable.AddPaging(_options, arguments);
            }

            return executable;
        }

        async ValueTask<IPage> IPagingHandler.SliceAsync(
            IResolverContext context,
            object source)
        {
            CursorPagingArguments arguments = CreatePagingArguments(context);

            return await SliceAsync(context, source, arguments).ConfigureAwait(false);
        }

        protected abstract ValueTask<Connection> SliceAsync(
            IResolverContext context,
            object source,
            CursorPagingArguments arguments);

        private CursorPagingArguments CreatePagingArguments(IResolverContext context)
        {
            int? first = context.ArgumentValue<int?>(First);
            int? last = context.ArgumentValue<int?>(Last);

            if (first is null && last is null)
            {
                first = DefaultPageSize;
            }

            return new CursorPagingArguments(
                first,
                last,
                context.ArgumentValue<string>(After),
                context.ArgumentValue<string>(Before));
        }
    }
}

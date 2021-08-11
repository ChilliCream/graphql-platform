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
            RequirePagingBoundaries = options.RequirePagingBoundaries ?? false;

            if (MaxPageSize < DefaultPageSize)
            {
                DefaultPageSize = MaxPageSize;
            }
        }

        /// <summary>
        /// Gets the default page size.
        /// </summary>
        protected int DefaultPageSize { get; }

        /// <summary>
        /// Gets max allowed page size.
        /// </summary>
        protected int MaxPageSize { get; }

        /// <summary>
        /// Defines if the paging middleware shall require the
        /// API consumer to specify paging boundaries.
        /// </summary>
        protected bool RequirePagingBoundaries { get; }

        public void ValidateContext(IResolverContext context)
        {
            var first = context.ArgumentValue<int?>(CursorPagingArgumentNames.First);
            var last = context.ArgumentValue<int?>(CursorPagingArgumentNames.Last);

            if (RequirePagingBoundaries && first is null && last is null)
            {
                // TODO : Error resources
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("You must provide a `first` or `last` value to properly paginate the `{0}` connection.")
                        .Build());
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
            var first = context.ArgumentValue<int?>(CursorPagingArgumentNames.First);
            var last = context.ArgumentValue<int?>(CursorPagingArgumentNames.Last);

            if (first is null && last is null)
            {
                first = DefaultPageSize;
            }

            var arguments = new CursorPagingArguments(
                first,
                last,
                context.ArgumentValue<string?>(CursorPagingArgumentNames.After),
                context.ArgumentValue<string?>(CursorPagingArgumentNames.Before));

            return await SliceAsync(context, source, arguments).ConfigureAwait(false);
        }

        protected abstract ValueTask<Connection> SliceAsync(
            IResolverContext context,
            object source,
            CursorPagingArguments arguments);
    }
}

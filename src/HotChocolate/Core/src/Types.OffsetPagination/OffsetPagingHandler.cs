using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Pagination
{
    public abstract class OffsetPagingHandler: IPagingHandler
    {
        protected OffsetPagingHandler(PagingSettings settings)
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
            int? take = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Take);

            if (take > MaxPageSize)
            {
                throw ThrowHelper.OffsetPagingHandler_MaxPageSize();
            }
        }

        async ValueTask<IPage> IPagingHandler.SliceAsync(
            IResolverContext context,
            object source)
        {
            int? skip = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Skip);
            int? take = context.ArgumentValue<int?>(OffsetPagingArgumentNames.Take);
            var arguments = new OffsetPagingArguments(skip, take ?? DefaultPageSize);

            return await SliceAsync(context, source, arguments).ConfigureAwait(false);
        }

        protected abstract ValueTask<CollectionSegment> SliceAsync(
            IResolverContext context,
            object source,
            OffsetPagingArguments arguments);
    }
}

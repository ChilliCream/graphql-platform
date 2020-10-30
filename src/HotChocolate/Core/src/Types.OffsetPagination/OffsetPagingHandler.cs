using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
using static HotChocolate.Types.Pagination.OffsetPagingArgumentNames;

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// Represents an offset paging handler, which can be implemented to
    /// create optimized pagination for data sources.
    ///
    /// The paging handler will be used by the paging middleware to slice the data.
    /// </summary>
    public abstract class OffsetPagingHandler : IPagingHandler
    {
        private readonly PagingOptions _options;

        protected OffsetPagingHandler(PagingOptions options)
        {
            _options = new PagingOptions
            {
                DefaultPageSize = options.DefaultPageSize ?? PagingDefaults.DefaultPageSize,
                MaxPageSize = options.MaxPageSize ?? PagingDefaults.MaxPageSize,
                IncludeTotalCount =
                    options.IncludeTotalCount ?? PagingDefaults.IncludeTotalCount,
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

        /// <summary>
        /// Ensures that the arguments passed in by the user are valid and
        /// do not try to consume more items per page as specified by
        /// <see cref="MaxPageSize"/>.
        /// </summary>
        /// <param name="context">
        /// The resolver context of the execution field.
        /// </param>
        public void ValidateContext(IResolverContext context)
        {
            int? take = context.ArgumentValue<int?>(Take);

            if (take > MaxPageSize)
            {
                throw ThrowHelper.OffsetPagingHandler_MaxPageSize();
            }
        }

        public IExecutable ApplyExecutable(IResolverContext context, IExecutable executable)
        {
            if (executable is IOffsetPagingExecutable offsetPagingExecutable)
            {
                OffsetPagingArguments arguments = CreatePagingArguments(context);

                var includeTotalCount = IncludeTotalCount && ShouldIncludeTotalCount(context);

                return offsetPagingExecutable.AddPaging(_options, arguments, includeTotalCount);
            }

            return executable;
        }

        async ValueTask<IPage> IPagingHandler.SliceAsync(
            IResolverContext context,
            object source)
        {
            OffsetPagingArguments arguments = CreatePagingArguments(context);
            return await SliceAsync(context, source, arguments).ConfigureAwait(false);
        }

        /// <summary>
        /// The algorithm defining how to slice data of the specified <paramref name="source"/>.
        /// </summary>
        /// <param name="context">
        /// The resolver context of the execution field.
        /// </param>
        /// <param name="source">
        /// The object representing the data source, collection, or query builder.
        /// </param>
        /// <param name="arguments">
        /// The paging arguments provided by the user.
        /// </param>
        /// <returns>
        /// The <see cref="CollectionSegment"/> representing
        /// the slice of items belonging to the requested page.
        /// </returns>
        protected abstract ValueTask<CollectionSegment> SliceAsync(
            IResolverContext context,
            object source,
            OffsetPagingArguments arguments);

        private OffsetPagingArguments CreatePagingArguments(IResolverContext context)
        {
            int? skip = context.ArgumentValue<int?>(Skip);
            int? take = context.ArgumentValue<int?>(Take);
            return new OffsetPagingArguments(skip, take ?? DefaultPageSize);
        }

        /// <summary>
        /// Checks if the selection set contains totalCount
        /// </summary>
        /// <param name="context">
        /// The resolver context of the execution field.
        /// </param>
        /// <returns>
        /// Returns true when the selection set contains totalCount
        /// </returns>
        public static bool ShouldIncludeTotalCount(
            IResolverContext context)
        {
            if (context.Field.Type is ObjectType objectType &&
                context.FieldSelection.SelectionSet is {} selectionSet)
            {
                IReadOnlyList<IFieldSelection> selections = context
                    .GetSelections(objectType, selectionSet, true);

                for (var i = 0; i < selections.Count; i++)
                {
                    if (selections[i].Field.Name.Value is "totalCount")
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

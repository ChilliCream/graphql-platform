using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// Represents the default paging handler for in-memory collections and queryables.
    /// </summary>
    /// <typeparam name="TItemType">
    /// The entity type.
    /// </typeparam>
    public class QueryableOffsetPagingHandler<TItemType>
        : OffsetPagingHandler
    {
        public QueryableOffsetPagingHandler(PagingOptions options)
            : base(options)
        {
        }

        protected override ValueTask<CollectionSegment> SliceAsync(
            IResolverContext context,
            object source,
            OffsetPagingArguments arguments)
        {
            return source switch
            {
                IQueryable<TItemType> q => ResolveAsync(context, q, arguments),
                IEnumerable<TItemType> e => ResolveAsync(context, e.AsQueryable(), arguments),
                IExecutable<TItemType> ex => SliceAsync(context, ex.Source, arguments),
                _ => throw new GraphQLException("Cannot handle the specified data source.")
            };
        }

        private async ValueTask<CollectionSegment> ResolveAsync(
            IResolverContext context,
            IQueryable<TItemType> queryable,
            OffsetPagingArguments arguments = default)
        {
            OffsetPagingHelper.CountAsync<IQueryable<TItemType>> getTotalCount =
                (_, _) => throw new InvalidOperationException();

            // TotalCount is one of the heaviest operations. It is only necessary to load totalCount
            // when it is enabled (IncludeTotalCount) and when it is contained in the selection set.
            if (IncludeTotalCount &&
                context.Field.Type is ObjectType objectType &&
                context.Selection.SyntaxNode.SelectionSet is { } selectionSet)
            {
                IReadOnlyList<IFieldSelection> selections = context
                    .GetSelections(objectType, selectionSet, true);

                var includeTotalCount = false;
                for (var i = 0; i < selections.Count; i++)
                {
                    if (selections[i].Field.Name.Value is "totalCount")
                    {
                        includeTotalCount = true;
                        break;
                    }
                }

                // When totalCount is included in the selection set we prefetch it, then capture the
                // count in a variable, to pass it into the clojure
                if (includeTotalCount)
                {
                    var captureCount = queryable.Count();
                    getTotalCount = (_, _) => new ValueTask<int>(captureCount);
                }
            }

            return await OffsetPagingHelper.ApplyPagination(
                queryable,
                arguments,
                (x, skip) => x.Skip(skip),
                (x, take) => x.Take(take),
                OffsetPagingHelper.ExecuteEnumerable,
                getTotalCount,
                context.RequestAborted);
        }
    }
}

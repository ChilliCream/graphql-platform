using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents the default paging handler for in-memory collections and queryables.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type.
/// </typeparam>
public class QueryableOffsetPagingHandler<TEntity>
    : OffsetPagingHandler
{
    private readonly QueryableOffsetPagination<TEntity> _pagination = new();

    public QueryableOffsetPagingHandler(PagingOptions options)
        : base(options)
    {
    }

    protected override ValueTask<CollectionSegment> SliceAsync(
        IResolverContext context,
        object source,
        OffsetPagingArguments arguments)
    {
        CancellationToken ct = context.RequestAborted;
        return source switch
        {
            IQueryable<TEntity> q => ResolveAsync(context, q, arguments, ct),
            IEnumerable<TEntity> e => ResolveAsync(context, e.AsQueryable(), arguments, ct),
            IExecutable<TEntity> ex => SliceAsync(context, ex.Source, arguments),
            _ => throw new GraphQLException("Cannot handle the specified data source.")
        };
    }

    private async ValueTask<CollectionSegment> ResolveAsync(
        IResolverContext context,
        IQueryable<TEntity> source,
        OffsetPagingArguments arguments = default,
        CancellationToken cancellationToken = default)
    {
        // When totalCount is included in the selection set we prefetch it, then capture the
        // count in a variable, to pass it into the handler
        int? totalCount = null;

        // TotalCount is one of the heaviest operations. It is only necessary to load totalCount
        // when it is enabled (IncludeTotalCount) and when it is contained in the selection set.

        var totalCountInSelection = false;
        var itemsInSelection = false;

        if (context.Selection.Type is ObjectType objectType &&
            context.Selection.SyntaxNode.SelectionSet is { } selectionSet)
        {
            IReadOnlyList<IFieldSelection> selections =
                context.GetSelections(objectType, selectionSet, true);

            if (IncludeTotalCount)
            {
                for (var i = 0; i < selections.Count; i++)
                {
                    if (selections[i].Field.Name.Value == "totalCount")
                    {
                        totalCountInSelection = true;
                        totalCount = source.Count();
                        break;
                    }
                }
            }

            for (var i = 0; i < selections.Count; i++)
            {
                if (selections[i].Field.Name.Value == "items")
                {
                    itemsInSelection = true;
                    break;
                }
            }
        }


        return await _pagination
            .ApplyPaginationAsync(source, arguments, totalCount, totalCountInSelection, itemsInSelection, cancellationToken)
            .ConfigureAwait(false);
    }
}

using System.Collections.Immutable;
using HotChocolate.Pagination.Expressions;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using HotChocolate.Types.Pagination.Utilities;
using Microsoft.EntityFrameworkCore;
using static HotChocolate.Data.Properties.EntityFrameworkResources;

namespace HotChocolate.Data.Pagination;

internal sealed class EfQueryableCursorPagingHandler<TEntity>(PagingOptions options)
    : CursorPagingHandler(options)
{
    protected override ValueTask<Connection> SliceAsync(
        IResolverContext context,
        object source,
        CursorPagingArguments arguments)
        => SliceAsync(context, CreateExecutable(source), arguments);

    private async ValueTask<Connection> SliceAsync(
        IResolverContext context,
        IQueryableExecutable<TEntity> executable,
        CursorPagingArguments arguments)
    {
        var query = executable.Source;
        var keys = ParseDataSetKeys(query);
        var forward = arguments.Last is null;
        var requestedCount = int.MaxValue;
        var fetchCount = 0;

        if (keys.Length == 0)
        {
            throw new ArgumentException(
                EfQueryableCursorPagingHandler_SliceAsync_NoOrder,
                nameof(executable));
        }

        if (arguments.Last is not null && arguments.First is not null)
        {
            throw new ArgumentException(
                EfQueryableCursorPagingHandler_SliceAsync_FirstOrLast,
                nameof(arguments));
        }

        if (arguments.After is not null)
        {
            var cursor = CursorParser.Parse(arguments.After, keys);
            query = query.Where(ExpressionHelpers.BuildWhereExpression<TEntity>(keys, cursor, forward));
        }

        if (arguments.Before is not null)
        {
            var cursor = CursorParser.Parse(arguments.Before, keys);
            query = query.Where(ExpressionHelpers.BuildWhereExpression<TEntity>(keys, cursor, forward));
        }

        if (arguments.First is not null)
        {
            query = query.Take(arguments.First.Value + 1);
            requestedCount = arguments.First.Value;
        }

        if (arguments.Last is not null)
        {
            query = query.Reverse().Take(arguments.Last.Value + 1);
            requestedCount = arguments.Last.Value;
        }

        context.SetOriginalQuery(executable.Source);
        context.SetSlicedQuery(query);

        var pagingFlags = context.GetPagingFlags(IncludeTotalCount);
        var countRequired = (pagingFlags & PagingFlags.TotalCount) == PagingFlags.TotalCount;
        var edgesRequired = (pagingFlags & PagingFlags.Edges) == PagingFlags.Edges;
        int? totalCount = null;

        if (!edgesRequired)
        {
            if(countRequired)
            {
                totalCount ??= await executable.CountAsync(context.RequestAborted);
            }

            return new Connection<TEntity>(ConnectionPageInfo.Empty, totalCount ?? -1);
        }

        var builder = ImmutableArray.CreateBuilder<Edge<TEntity>>();

#if DEBUG
        if (context.ContextData.ContainsKey("printSQL"))
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("sql", query.ToQueryString());
        }

#endif
        if (countRequired)
        {
            var originalQuery = executable.Source;
            var combinedQuery = query.Select(t => new { TotalCount = originalQuery.Count(), Item = t });

            await foreach (var item in executable
                .WithSource(combinedQuery)
                .ToAsyncEnumerable(context.RequestAborted)
                .ConfigureAwait(false))
            {
                fetchCount++;

                if (fetchCount > requestedCount)
                {
                    break;
                }

                builder.Add(new Edge<TEntity>(item.Item, CursorFormatter.Format(item.Item, keys)));
                totalCount ??= item.TotalCount;
            }
        }
        else
        {
            await foreach (var item in executable
                .WithSource(query)
                .ToAsyncEnumerable(context.RequestAborted)
                .ConfigureAwait(false))
            {
                fetchCount++;

                if (fetchCount > requestedCount)
                {
                    break;
                }

                builder.Add(new Edge<TEntity>(item, CursorFormatter.Format(item, keys)));
            }
        }

        if (builder.Count == 0)
        {
            return Connection.Empty<TEntity>();
        }

        if (!forward)
        {
            builder.Reverse();
        }

        return CreatePage(
            builder.ToImmutable(),
            arguments,
            fetchCount,
            totalCount);
    }

    private static Connection<T> CreatePage<T>(
        ImmutableArray<Edge<T>> items,
        CursorPagingArguments arguments,
        int fetchCount,
        int? totalCount = null)
    {
        var hasPrevious = false;
        var hasNext = false;

        // if we skipped over an item, and we have fetched some items
        // than we have a previous page as we skipped over at least
        // one item.
        if (arguments.After is not null && fetchCount > 0)
        {
            hasPrevious = true;
        }

        // if we required the last 5 items of a dataset and overfetch by 1
        // than we have a previous page.
        if (arguments.Last is not null && fetchCount > arguments.Last)
        {
            hasPrevious = true;
        }

        // if we request the first 5 items of a dataset with or without cursor
        // and we over-fetched by 1 item we have a next page.
        if (arguments.First is not null && fetchCount > arguments.First)
        {
            hasNext = true;
        }

        // if we fetched anything before an item we know that here is at least one more item.
        if (arguments.Before is not null)
        {
            hasNext = true;
        }

        var pageInfo = new ConnectionPageInfo(
            hasNext,
            hasPrevious,
            items[0].Cursor,
            items[^1].Cursor);

        return new Connection<T>(
            items,
            pageInfo,
            totalCount ?? -1);
    }

    private static IQueryableExecutable<TEntity> CreateExecutable(object source)
        => source switch
        {
            IQueryable<TEntity> q => q.AsDbContextExecutable(),
            IEnumerable<TEntity> e => e.AsQueryable().AsDbContextExecutable(),
            IQueryableExecutable<TEntity> e => e,
            _ => throw new InvalidOperationException(EfQueryableCursorPagingHandler_SourceNotSupported),
        };

    private static CursorKey[] ParseDataSetKeys<T>(IQueryable<T> source)
    {
        var parser = new CursorKeyParser();
        parser.Visit(source.Expression);
        return parser.Keys.ToArray();
    }
}

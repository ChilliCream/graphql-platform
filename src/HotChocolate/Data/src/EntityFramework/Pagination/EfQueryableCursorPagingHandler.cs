using System.Collections.Immutable;
using HotChocolate.Data;
using HotChocolate.Pagination.Expressions;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Types.Pagination;

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
                "In order to use cursor pagination, you must specify at least on key using the `OrderBy` method.",
                nameof(executable));
        }

        if (arguments.Last is not null && arguments.First is not null)
        {
            throw new ArgumentException(
                "You can specify either `first` or `last`, but not both as this can lead to unpredictable results.",
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
            query = query.Reverse().Take(arguments.Last.Value);
            requestedCount = arguments.Last.Value;
        }

        var builder = ImmutableArray.CreateBuilder<Edge<TEntity>>();

#if DEBUG
        if (context.ContextData.ContainsKey("printSQL"))
        {
            ((IMiddlewareContext)context).OperationResult.SetExtension("sql", query.ToQueryString());
        }

#endif
        await foreach (var item in executable
            .WithSource(query)
            .ToAsyncEnumerable(context.RequestAborted)
            .ConfigureAwait(false))
        {
            builder.Add(new Edge<TEntity>(item, CursorFormatter.Format(item, keys)));
            fetchCount++;

            if (fetchCount >= requestedCount)
            {
                break;
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
            keys,
            fetchCount);
    }

    private static Connection<T> CreatePage<T>(
        ImmutableArray<Edge<T>> items,
        CursorPagingArguments arguments,
        CursorKey[] keys,
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
            IQueryable<TEntity> q => q.ToExecutable(),
            IEnumerable<TEntity> e => e.AsQueryable().ToExecutable(),
            IQueryableExecutable<TEntity> e => e,
            _ => throw new InvalidOperationException("Cannot handle the specified data source."),
        };

    private static CursorKey[] ParseDataSetKeys<T>(IQueryable<T> source)
    {
        var parser = new CursorKeyParser();
        parser.Visit(source.Expression);
        return parser.Keys.ToArray();
    }
}

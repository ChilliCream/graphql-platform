using System.Collections.Immutable;
using HotChocolate.Pagination.Expressions;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination;

internal sealed class QueryableCursorPagingHandler2<TEntity>(PagingOptions options)
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

        if(keys.Length == 0)
        {
            throw new ArgumentException(
                "In order to use cursor pagination, you must specify at least on key using the `OrderBy` method.",
                nameof(executable));
        }

        if(arguments.Last is not null && arguments.First is not null)
        {
            throw new ArgumentException(
                "You can specify either `first` or `last`, but not both as this can lead to unpredictable results.",
                nameof(arguments));
        }

        var forward = arguments.Last is null;

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
            query = query.Take(arguments.First.Value);
        }

        if (arguments.Last is not null)
        {
            query = query.Reverse().Take(arguments.Last.Value);
        }

        var builder = ImmutableArray.CreateBuilder<Edge<TEntity>>();

        await foreach(var item in executable
            .WithSource(query)
            .ToAsyncEnumerable(context.RequestAborted)
            .ConfigureAwait(false))
        {
            builder.Add(new Edge<TEntity>(item, CursorFormatter.Format(item, keys)));
        }

        /*

#if NET7_0_OR_GREATER

#else

#endif

        if(result.Count == 0)
        {
            return Page<T>.Empty;
        }

        if (!forward)
        {
            result.Reverse();
        }

        */

        throw new Exception();
    }

    private IQueryableExecutable<TEntity> CreateExecutable(object source)
        => source switch
        {
            IQueryable<TEntity> q => Executable.From(q),
            IEnumerable<TEntity> e => Executable.From(e.AsQueryable()),
            IQueryableExecutable<TEntity> ex => ex,
            _ => throw new GraphQLException("Cannot handle the specified data source."),
        };

    private static CursorKey[] ParseDataSetKeys<T>(IQueryable<T> source)
    {
        var parser = new CursorKeyParser();
        parser.Visit(source.Expression);
        return parser.Keys.ToArray();
    }
}

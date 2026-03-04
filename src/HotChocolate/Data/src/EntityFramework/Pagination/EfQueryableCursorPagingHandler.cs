using System.Collections.Immutable;
using System.Reflection;
using GreenDonut.Data;
using GreenDonut.Data.Cursors;
using GreenDonut.Data.Expressions;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using HotChocolate.Types.Pagination.Utilities;
using Microsoft.EntityFrameworkCore.Infrastructure;
#if DEBUG
using Microsoft.EntityFrameworkCore;
#endif
using static HotChocolate.Data.Properties.EntityFrameworkResources;

namespace HotChocolate.Data.Pagination;

internal sealed class EfQueryableCursorPagingHandler<TEntity>(PagingOptions options)
    : CursorPagingHandler(options)
{
    private const BindingFlags BindingFlagsInstance =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private readonly NullOrdering _nullOrdering = options.NullOrdering;

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
        var nullOrdering = ResolveNullOrdering(query, executable.IsInMemory, _nullOrdering);
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
            var (whereExpr, _) =
                ExpressionHelpers.BuildWhereExpression<TEntity>(
                    keys,
                    cursor,
                    true,
                    nullOrdering);
            query = query.Where(whereExpr);
        }

        if (arguments.Before is not null)
        {
            var cursor = CursorParser.Parse(arguments.Before, keys);
            var (whereExpr, _) =
                ExpressionHelpers.BuildWhereExpression<TEntity>(
                    keys,
                    cursor,
                    false,
                    nullOrdering);
            query = query.Where(whereExpr);
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
            if (countRequired)
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
            _ => throw new InvalidOperationException(EfQueryableCursorPagingHandler_SourceNotSupported)
        };

    private static CursorKey[] ParseDataSetKeys<T>(IQueryable<T> source)
    {
        var parser = new CursorKeyParser();
        parser.Visit(source.Expression);
        return parser.Keys.ToArray();
    }

    private static NullOrdering ResolveNullOrdering(
        IQueryable<TEntity> query,
        bool isInMemory,
        NullOrdering configured)
    {
        if (configured is not NullOrdering.Unspecified)
        {
            return configured;
        }

        // LINQ-to-Objects sorts null values first in ascending order.
        if (isInMemory)
        {
            return NullOrdering.NativeNullsFirst;
        }

        var providerName = TryGetProviderName(query);

        if (providerName is not null)
        {
            return providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
                ? NullOrdering.NativeNullsLast
                : NullOrdering.NativeNullsFirst;
        }

        // EF query providers do not always expose IServiceProvider via IInfrastructure.
        // In that case we inspect provider-specific services held by QueryCompiler.
        if (IsNpgsqlProvider(query.Provider))
        {
            return NullOrdering.NativeNullsLast;
        }

        // Most other EF providers used by Hot Chocolate (for example SQL Server/SQLite)
        // sort null values first in ascending order by default.
        return NullOrdering.NativeNullsFirst;
    }

    private static string? TryGetProviderName(IQueryable<TEntity> query)
    {
        if (TryGetProviderName(query as IInfrastructure<IServiceProvider>) is { } providerName)
        {
            return providerName;
        }

        if (TryGetProviderName(query.Provider as IInfrastructure<IServiceProvider>) is { } providerNameFromProvider)
        {
            return providerNameFromProvider;
        }

        return null;
    }

    private static string? TryGetProviderName(IInfrastructure<IServiceProvider>? infrastructure)
        => infrastructure?
            .Instance
            .GetService(typeof(ICurrentDbContext))
                is ICurrentDbContext currentDbContext
            ? currentDbContext.Context.Database.ProviderName
            : null;

    private static bool IsNpgsqlProvider(IQueryProvider provider)
    {
        if (IsNpgsqlType(provider.GetType()))
        {
            return true;
        }

        if (TryGetFieldValue(provider, "queryCompiler") is not { } queryCompiler)
        {
            return false;
        }

        if (IsNpgsqlType(queryCompiler.GetType()))
        {
            return true;
        }

        if (TryGetFieldValue(queryCompiler, "compiledQueryCacheKeyGenerator") is { } keyGenerator
            && IsNpgsqlType(keyGenerator.GetType()))
        {
            return true;
        }

        // Fallback for provider-specific services exposed through other query compiler fields.
        foreach (var field in queryCompiler.GetType().GetFields(BindingFlagsInstance))
        {
            if (field.GetValue(queryCompiler) is { } value
                && IsNpgsqlType(value.GetType()))
            {
                return true;
            }
        }

        return false;
    }

    private static object? TryGetFieldValue(object instance, string fieldNameContains)
    {
        foreach (var field in instance.GetType().GetFields(BindingFlagsInstance))
        {
            if (field.Name.Contains(fieldNameContains, StringComparison.OrdinalIgnoreCase))
            {
                return field.GetValue(instance);
            }
        }

        return null;
    }

    private static bool IsNpgsqlType(Type type)
        => (type.FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ?? false)
            || (type.Assembly.GetName().Name?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ?? false);
}

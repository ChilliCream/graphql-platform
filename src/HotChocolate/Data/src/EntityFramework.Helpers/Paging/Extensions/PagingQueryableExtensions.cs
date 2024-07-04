using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using HotChocolate.Pagination;

namespace HotChocolate.Data;

/// <summary>
/// Provides extension methods to page a queryable.
/// </summary>
public static class PagingQueryableExtensions
{
    private static readonly MethodInfo _createAndConvert = typeof(PagingQueryableExtensions)
        .GetMethod(nameof(CreateAndConvertParameter), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly ConcurrentDictionary<Type, Func<object?, Expression>> _cachedConverters = new();

    /// <summary>
    /// Executes a query with paging and returns the selected page.
    /// </summary>
    /// <param name="source">
    /// The queryable to be paged.
    /// </param>
    /// <param name="arguments">
    /// The paging arguments.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <typeparam name="T">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns a page of items.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified.
    /// </exception>
    public static async ValueTask<Page<T>> ToPageAsync<T>(
        this IQueryable<T> source,
        PagingArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var keys = ParseDataSetKeys(source);

        if(keys.Length == 0)
        {
            throw new ArgumentException(
                "In order to use cursor pagination, you must specify at least on key using the `OrderBy` method.",
                nameof(source));
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
            source = source.Where(BuildWhereExpression<T>(keys, cursor, forward));
        }

        if (arguments.Before is not null)
        {
            var cursor = CursorParser.Parse(arguments.Before, keys);
            source = source.Where(BuildWhereExpression<T>(keys, cursor, forward));
        }

        if (arguments.First is not null)
        {
            source = source.Take(arguments.First.Value);
        }

        if (arguments.Last is not null)
        {
            source = source.Reverse().Take(arguments.Last.Value);
        }

        var result = await source.ToListAsync(cancellationToken);

        if(result.Count == 0)
        {
            return Page<T>.Empty;
        }

        if (!forward)
        {
            result.Reverse();
        }

        return CreatePage(result, arguments, keys);
    }

    /// <summary>
    /// Executes a batch query with paging and returns the selected pages for each parent.
    /// </summary>
    /// <param name="source">
    /// The queryable to be paged.
    /// </param>
    /// <param name="keySelector">
    /// A function to select the key of the parent.
    /// </param>
    /// <param name="arguments">
    /// The paging arguments.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <typeparam name="T">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The type of the parent key.
    /// </typeparam>
    /// <typeparam name="TProperty">
    /// The type of the property that is being paged.
    /// </typeparam>
    /// <returns>
    /// Returns a dictionary of pages for each parent.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// If the result is not a list or an enumerable.
    /// </exception>
    public static async ValueTask<Dictionary<PageKey<TKey>, Page<TProperty>>> ToBatchPageAsync<T, TKey, TProperty>(
        this IIncludableQueryable<T, IOrderedEnumerable<TProperty>> source,
        Func<T, TKey> keySelector,
        PagingArguments arguments,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var rewriter = new BatchQueryRewriter<TProperty>(arguments);
        var expression = rewriter.Visit(source.Expression);
        var list = await source.Provider.CreateQuery<T>(expression).ToListAsync(cancellationToken);
        var result = new Dictionary<PageKey<TKey>, Page<TProperty>>();

        foreach (var group in list)
        {
            var key = new PageKey<TKey>(keySelector(group), arguments);

            switch (rewriter.ResultProperty.GetValue(group))
            {
                case IReadOnlyList<TProperty> resultList:
                    result.Add(key, CreatePage(resultList, arguments, rewriter.Keys));
                    break;

                case IEnumerable<TProperty> resultEnumerable:
                    result.Add(key, CreatePage(resultEnumerable.ToArray(), arguments, rewriter.Keys));
                    break;

                default:
                    throw new InvalidOperationException(
                        "The result must be a list or an enumerable.");
            }
        }

        return result;
    }

    private static Page<T> CreatePage<T>(IReadOnlyList<T> items, PagingArguments arguments, DataSetKey[] keys)
    {
        var hasPrevious = arguments.First is not null && items.Count > 0 ||
            arguments.Last is not null && items.Count > arguments.Last;
        var hasNext = arguments.First is not null && items.Count > arguments.First ||
            arguments.Last is not null && items.Count > 0;

        return new Page<T>(items, hasNext, hasPrevious, item => CursorFormatter.Format(item, keys));
    }

    private static DataSetKey[] ParseDataSetKeys<T>(IQueryable<T> source)
    {
        var parser = new DataSetKeyParser();
        parser.Visit(source.Expression);
        return parser.Keys.ToArray();
    }

    internal static Expression<Func<T, bool>> BuildWhereExpression<T>(
        DataSetKey[] keys,
        object[] cursor,
        bool forward)
    {
        if (keys == null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        if (cursor == null)
        {
            throw new ArgumentNullException(nameof(cursor));
        }

        if (keys.Length == 0)
        {
            throw new ArgumentException("At least one key must be specified.", nameof(keys));
        }

        if (keys.Length != cursor.Length)
        {
            throw new ArgumentException("The number of keys must match the number of values.", nameof(cursor));
        }

        var cursorExpr = new Expression[cursor.Length];
        for (var i = 0; i < cursor.Length; i++)
        {
            cursorExpr[i] = CreateParameter(cursor[i], keys[i].Property.PropertyType);
        }

        var handled = new List<DataSetKey>();
        Expression? expression = null;

        var entity = Expression.Parameter(typeof(T), "t");
        var zero = Expression.Constant(0);

        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            Expression? current = null;
            Expression keyExpr;

            for (var j = 0; j < handled.Count; j++)
            {
                var handledKey = handled[j];

                keyExpr =
                    Expression.Equal(
                        Expression.Call(
                            Expression.Property(entity, handledKey.Property),
                            handledKey.CompareMethod,
                            cursorExpr[j]),
                        zero);

                current = current is null
                    ? keyExpr
                    : Expression.AndAlso(current, keyExpr);
            }

            var greaterThan = forward
                ? key.Ascending
                : !key.Ascending;

            keyExpr =
                greaterThan
                    ? Expression.GreaterThan(
                        Expression.Call(
                            Expression.Property(entity, key.Property),
                            key.CompareMethod,
                            cursorExpr[i]),
                        zero)
                    : Expression.LessThan(
                        Expression.Call(
                            Expression.Property(entity, key.Property),
                            key.CompareMethod,
                            cursorExpr[i]),
                        zero);

            current = current is null
                ? keyExpr
                : Expression.AndAlso(current, keyExpr);
            expression = expression is null
                ? current
                : Expression.OrElse(expression, current);
            handled.Add(key);
        }

        return Expression.Lambda<Func<T, bool>>(expression!, entity);
    }

    private static Expression CreateParameter(object? value, Type type)
    {
        var converter = _cachedConverters.GetOrAdd(
            type,
            t =>
            {
                var method = _createAndConvert.MakeGenericMethod(t);
                return v => (Expression)method.Invoke(null, [v,])!;
            });

        return converter(value);
    }

    private static Expression CreateAndConvertParameter<T>(object value)
    {
        Expression<Func<T>> lambda = () => (T)value;
        return lambda.Body;
    }
}

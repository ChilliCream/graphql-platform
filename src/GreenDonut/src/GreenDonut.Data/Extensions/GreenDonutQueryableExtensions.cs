using System.Linq.Expressions;
using System.Reflection;
using GreenDonut.Data;
using static GreenDonut.Data.Internal.ExpressionHelpers;

// ReSharper disable once CheckNamespace
namespace System.Linq;

/// <summary>
/// Provides extension methods to integrate <see cref="IQueryable{T}"/>
/// for <see cref="SortDefinition{T}"/> and <see cref="QueryContext{T}"/>.
/// </summary>
public static class GreenDonutQueryableExtensions
{
    private static readonly MethodInfo s_selectMethod =
        typeof(Enumerable)
            .GetMethods()
            .Where(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2)
            .First(m => m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>));

    /// <summary>
    /// Applies the selector from the DataLoader state to a queryable.
    /// </summary>
    /// <param name="query">
    ///  The queryable to apply the selector to.
    /// </param>
    /// <param name="keySelector">
    /// The DataLoader key.
    /// </param>
    /// <param name="builder">
    ///  The selector builder.
    /// </param>
    /// <typeparam name="T">
    /// The queryable type.
    /// </typeparam>
    /// <returns>
    /// Returns a selector query on which a key must be applied to fetch the data.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="query"/> is <c>null</c>.
    /// </exception>
    public static IQueryable<T> Select<T>(
        this IQueryable<T> query,
        Expression<Func<T, object?>> keySelector,
        ISelectorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(builder);

        var selector = builder.TryCompile<T>();

        if (selector is not null)
        {
            query = query.Select(Combine(selector, Rewrite(keySelector)));
        }

        return query;
    }

    /// <summary>
    /// Applies the selector from the DataLoader state to a queryable.
    /// </summary>
    /// <typeparam name="T">
    /// The queryable type.
    /// </typeparam>
    /// <param name="query">
    /// The queryable to apply the selector to.
    /// </param>
    /// <param name="keySelector">
    /// The DataLoader key.
    /// </param>
    /// <param name="selector">
    /// The selector.
    /// </param>
    /// <returns>
    /// Returns the query with the selector applied.
    /// </returns>
    public static IQueryable<T> Select<T>(
        this IQueryable<T> query,
        Expression<Func<T, object?>> keySelector,
        Expression<Func<T, T>>? selector)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (selector is not null)
        {
            query = query.Select(Combine(selector, Rewrite(keySelector)));
        }

        return query;
    }

    /// <summary>
    /// Applies the selector from the DataLoader state to a queryable.
    /// </summary>
    /// <param name="query">
    /// The queryable to apply the selector to.
    /// </param>
    /// <param name="key">
    /// The DataLoader key.
    /// </param>
    /// <param name="list">
    /// The list selector.
    /// </param>
    /// <param name="elementSelector">
    /// The element selector.
    /// </param>
    /// <typeparam name="T">
    /// The queryable type.
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns a selector query on which a key must be applied to fetch the data.
    /// </returns>
    public static IQueryable<KeyValueResult<TKey, IEnumerable<TValue>>> Select<T, TKey, TValue>(
        this IQueryable<T> query,
        Expression<Func<T, TKey?>> key,
        Expression<Func<T, IEnumerable<TValue>>> list,
        ISelectorBuilder elementSelector)
    {
        // we first create a new parameter expression for the root as we need
        // a unified parameter for both expressions (key and list)
        var parameter = Expression.Parameter(typeof(T), "root");

        // next we replace the parameter within the key and list selectors with the
        // unified parameter.
        var rewrittenKey = ReplaceParameter(key, key.Parameters[0], parameter);
        var rewrittenList = ReplaceParameter(list, list.Parameters[0], parameter);

        // next we try to compile an element selector expression.
        var elementSelectorExpr = elementSelector.TryCompile<TValue>();

        // if we have an element selector to project properties on the list expression
        // we will need to combine this into the list expression.
        if (elementSelectorExpr is not null)
        {
            var selectMethod = s_selectMethod.MakeGenericMethod(typeof(TValue), typeof(TValue));

            rewrittenList = Expression.Lambda<Func<T, IEnumerable<TValue>>>(
                Expression.Call(
                    selectMethod,
                    rewrittenList.Body,
                    elementSelectorExpr),
                parameter);
        }

        // finally we combine key and list expression into a single selector expression
        var keyValueSelectorExpr = Expression.Lambda<Func<T, KeyValueResult<TKey, IEnumerable<TValue>>>>(
            Expression.MemberInit(
                Expression.New(typeof(KeyValueResult<TKey, IEnumerable<TValue>>)),
                Expression.Bind(
                    typeof(KeyValueResult<TKey, IEnumerable<TValue>>).GetProperty(
                        nameof(KeyValueResult<TKey, IEnumerable<TValue>>.Key))!,
                    rewrittenKey.Body),
                Expression.Bind(
                    typeof(KeyValueResult<TKey, IEnumerable<TValue>>).GetProperty(
                        nameof(KeyValueResult<TKey, IEnumerable<TValue>>.Value))!,
                    rewrittenList.Body)),
            parameter);

        // lastly we apply the selector expression to the queryable.
        return query.Select(keyValueSelectorExpr);
    }

    /// <summary>
    /// Applies the predicate from the DataLoader state to a queryable.
    /// </summary>
    /// <param name="query">
    /// The queryable to apply the predicate to.
    /// </param>
    /// <param name="builder">
    /// The predicate builder.
    /// </param>
    /// <typeparam name="T">
    /// The queryable type.
    /// </typeparam>
    /// <returns>
    /// Returns a query with the predicate applied, ready to fetch data with the key.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="query"/> is <c>null</c>.
    /// </exception>
    public static IQueryable<T> Where<T>(
        this IQueryable<T> query,
        IPredicateBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(builder);

        var predicate = builder.TryCompile<T>();

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query;
    }

    /// <summary>
    /// Applies the <paramref name="sortDefinition"/> to the queryable (if its not null)
    /// and returns an ordered queryable.
    /// </summary>
    /// <param name="queryable">
    /// The queryable that shall be ordered.
    /// </param>
    /// <param name="sortDefinition">
    /// The sort definition that shall be applied to the queryable.
    /// </param>
    /// <typeparam name="T">
    /// The type of the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns an ordered queryable.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="queryable"/> is <c>null</c>.
    /// </exception>
    public static IQueryable<T> OrderBy<T>(this IQueryable<T> queryable, SortDefinition<T>? sortDefinition)
    {
        ArgumentNullException.ThrowIfNull(queryable);

        if (sortDefinition is null || sortDefinition.Operations.Length == 0)
        {
            return queryable;
        }

        var first = sortDefinition.Operations[0];
        var query = first.ApplyOrderBy(queryable);

        for (var i = 1; i < sortDefinition.Operations.Length; i++)
        {
            query = sortDefinition.Operations[i].ApplyThenBy(query);
        }

        return query;
    }

    /// <summary>
    /// Applies the <paramref name="sortDefinition"/> to the queryable (if its not null)
    /// and returns an ordered queryable.
    /// </summary>
    /// <param name="queryable">
    /// The queryable that shall be ordered.
    /// </param>
    /// <param name="sortDefinition">
    /// The sort definition that shall be applied to the queryable.
    /// </param>
    /// <typeparam name="T">
    /// The type of the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns an ordered queryable.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="queryable"/> is <c>null</c>.
    /// </exception>
    public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> queryable, SortDefinition<T>? sortDefinition)
    {
        ArgumentNullException.ThrowIfNull(queryable);

        if (sortDefinition is null || sortDefinition.Operations.Length == 0)
        {
            return queryable;
        }

        for (var i = 0; i < sortDefinition.Operations.Length; i++)
        {
            queryable = sortDefinition.Operations[i].ApplyThenBy(queryable);
        }

        return queryable;
    }

    /// <summary>
    /// Applies a data context to the queryable.
    /// </summary>
    /// <param name="queryable">
    /// The queryable that shall be projected, filtered and sorted.
    /// </param>
    /// <param name="queryContext">
    /// The data context that shall be applied to the queryable.
    /// </param>
    /// <param name="modifySortDefinition">
    /// A delegate to modify the sort definition.
    /// </param>
    /// <typeparam name="T">
    /// The type of the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns a queryable that has the data context applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="queryable"/> is <c>null</c> or if <paramref name="queryContext"/> is <c>null</c>.
    /// </exception>
    public static IQueryable<T> With<T>(
        this IQueryable<T> queryable,
        QueryContext<T>? queryContext,
        Func<SortDefinition<T>, SortDefinition<T>>? modifySortDefinition = null)
    {
        ArgumentNullException.ThrowIfNull(queryable);

        if (queryContext is null)
        {
            return queryable;
        }

        var sorting = queryContext.Sorting;
        if(modifySortDefinition is not null)
        {
            sorting ??= SortDefinition<T>.Empty;
            sorting = modifySortDefinition(sorting);
        }

        if (queryContext.Predicate is not null)
        {
            queryable = queryable.Where(queryContext.Predicate);
        }

        if (sorting?.Operations.Length > 0)
        {
            queryable = queryable.OrderBy(sorting);
        }

        if (queryContext.Selector is not null)
        {
            queryable = queryable.Select(queryContext.Selector);
        }

        return queryable;
    }

    private static Expression<T> ReplaceParameter<T>(
        Expression<T> expression,
        ParameterExpression oldParameter,
        ParameterExpression newParameter)
        => (Expression<T>)new ReplaceParameterVisitor(oldParameter, newParameter).Visit(expression);
}

file sealed class ReplaceParameterVisitor(
    ParameterExpression oldParameter,
    ParameterExpression newParameter)
    : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
        => node == oldParameter
            ? newParameter
            : base.VisitParameter(node);
}

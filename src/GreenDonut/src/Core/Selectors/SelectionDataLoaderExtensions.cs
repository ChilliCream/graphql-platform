using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using static GreenDonut.ExpressionHelpers;

namespace GreenDonut.Selectors;

/// <summary>
/// Data loader extensions for projections.
/// </summary>
[Experimental(Experiments.Selectors)]
public static class SelectionDataLoaderExtensions
{
    private static readonly MethodInfo _selectMethod =
        typeof(Enumerable)
            .GetMethods()
            .Where(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2)
            .First(m => m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>));

    /// <summary>
    /// Branches a DataLoader and applies a selector to load the data.
    /// </summary>
    /// <param name="dataLoader">
    /// The DataLoader to branch.
    /// </param>
    /// <param name="selector">
    /// The data selector.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <typeparam name="TElement">
    /// The element type.
    /// </typeparam>
    /// <returns>
    /// Returns a branched DataLoader with the selector applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static IDataLoader<TKey, TValue> Select<TKey, TValue, TElement>(
        this IDataLoader<TKey, TValue> dataLoader,
        Expression<Func<TElement, TElement>>? selector)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (selector is null)
        {
            return dataLoader;
        }

        if (dataLoader.ContextData.TryGetValue(typeof(ISelectorBuilder).FullName!, out var value))
        {
            var context = (DefaultSelectorBuilder)value!;
            context.Add(selector);
            return dataLoader;
        }

        var branchKey = selector.ToString();
        return (ISelectionDataLoader<TKey, TValue>)dataLoader.Branch(branchKey, CreateBranch, selector);

        static IDataLoader CreateBranch(
            string key,
            IDataLoader<TKey, TValue> dataLoader,
            Expression<Func<TElement, TElement>> selector)
        {
            var branch = new SelectionDataLoader<TKey, TValue>(
                (DataLoaderBase<TKey, TValue>)dataLoader,
                key);
            var context = new DefaultSelectorBuilder();
            branch.ContextData = branch.ContextData.SetItem(typeof(ISelectorBuilder).FullName!, context);
            context.Add(selector);
            return branch;
        }
    }

    /// <summary>
    /// Includes a property in the query.
    /// </summary>
    /// <param name="dataLoader">
    /// The DataLoader to include the property in.
    /// </param>
    /// <param name="includeSelector">
    /// The property selector.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns the DataLoader with the property included.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Throws if the include selector is not a property selector.
    /// </exception>
    public static IDataLoader<TKey, TValue> Include<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        Expression<Func<TValue, object?>> includeSelector)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (includeSelector is null)
        {
            throw new ArgumentNullException(nameof(includeSelector));
        }

        if (includeSelector is not LambdaExpression lambda)
        {
            throw new ArgumentException(
                "The include selector must be a lambda expression.",
                nameof(includeSelector));
        }

        if (lambda.Body is not MemberExpression member
            || member.Member.MemberType != MemberTypes.Property)
        {
            throw new ArgumentException(
                "The include selector must be a property selector.",
                nameof(includeSelector));
        }

        var context = dataLoader.GetOrSetState(
            typeof(ISelectorBuilder).FullName!,
            _ => new DefaultSelectorBuilder());
        context.Add(Rewrite(includeSelector));
        return dataLoader;
    }

    /// <summary>
    /// Applies the selector from the DataLoader state to a queryable.
    /// </summary>
    /// <param name="query">
    ///  The queryable to apply the selector to.
    /// </param>
    /// <param name="key">
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
    public static IQueryable<T> Select<T>(this IQueryable<T> query,
        Expression<Func<T, object?>> key,
        ISelectorBuilder builder)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var selector = builder.TryCompile<T>();

        if (selector is not null)
        {
            query = query.Select(Combine(selector, Rewrite(key)));
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

        // if we have a element selector to project properties on the list expression
        // we will need to combine this into the list expression.
        if (elementSelectorExpr is not null)
        {
            var selectMethod = _selectMethod.MakeGenericMethod(typeof(TValue), typeof(TValue));

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

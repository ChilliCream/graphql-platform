#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace GreenDonut.Projections;

/// <summary>
/// Data loader extensions for projections.
/// </summary>
[Experimental(Experiments.Projections)]
public static class SelectionDataLoaderExtensions
{
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
    /// <returns>
    /// Returns a branched DataLoader with the selector applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static ISelectionDataLoader<TKey, TValue> Select<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        Expression<Func<TValue, TValue>> selector)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        DefaultSelectorBuilder<TValue> context;
        var branch = dataLoader.Branch(selector.ToString());
        if (branch.ContextData.TryGetValue(typeof(ISelectorBuilder).FullName!, out var value)
            && value is DefaultSelectorBuilder<TValue> casted)
        {
            context = casted;
        }
        else
        {
            context = new DefaultSelectorBuilder<TValue>();
        }

        context.Add(selector);
        branch.ContextData = branch.ContextData.SetItem(typeof(ISelectorBuilder).FullName!, context);
        return branch;
    }

    /// <summary>
    /// Adds another selector to the branched DataLoader.
    /// </summary>
    /// <param name="dataLoader">
    /// The branched DataLoader.
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
    /// <returns>
    /// Returns the branched DataLoader with the selector applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
    /// </exception>
    public static ISelectionDataLoader<TKey, TValue> Select<TKey, TValue>(
        this ISelectionDataLoader<TKey, TValue> dataLoader,
        Expression<Func<TValue, TValue>> selector)
        where TKey : notnull
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        var context = (DefaultSelectorBuilder<TValue>)dataLoader.ContextData[typeof(ISelectorBuilder).FullName!]!;
        context.Add(selector);
        return dataLoader;
    }

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

        DefaultSelectorBuilder<TValue> context;
        if (dataLoader.ContextData.TryGetValue(typeof(ISelectorBuilder).FullName!, out var value)
            && value is DefaultSelectorBuilder<TValue> casted)
        {
            context = casted;
        }
        else
        {
            context = new DefaultSelectorBuilder<TValue>();
        }

        context.Add(ExpressionHelpers.Rewrite(includeSelector));
        return dataLoader;
    }

    public static ISelectionDataLoader<TKey, TValue> Include<TKey, TValue>(
        this ISelectionDataLoader<TKey, TValue> dataLoader,
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

        var context = (DefaultSelectorBuilder<TValue>)dataLoader.ContextData[typeof(ISelectorBuilder).FullName!]!;
        context.Add(ExpressionHelpers.Rewrite(includeSelector));
        return dataLoader;
    }

    /// <summary>
    /// Applies the selector from the DataLoader state to a queryable.
    /// </summary>
    /// <param name="queryable">
    /// The queryable to apply the selector to.
    /// </param>
    /// <param name="builder">
    /// The selector builder.
    /// </param>
    /// <typeparam name="T">
    /// The queryable type.
    /// </typeparam>
    /// <returns>
    /// Returns a selector query on which a key must be applied to fetch the data.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Throws if <paramref name="queryable"/> is <c>null</c>.
    /// </exception>
    public static ISelectorQuery<T> Select<T>(
        this IQueryable<T> queryable,
        ISelectorBuilder builder)
    {
        if (queryable is null)
        {
            throw new ArgumentNullException(nameof(queryable));
        }

        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var selector = builder.TryCompile<T>();
        return new DefaultSelectorQuery<T>(queryable, selector);
    }
}
#endif

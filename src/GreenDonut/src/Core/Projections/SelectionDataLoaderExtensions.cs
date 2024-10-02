using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using static GreenDonut.Projections.ExpressionHelpers;

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
    public static IDataLoader<TKey, TValue> Select<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        Expression<Func<TValue, TValue>>? selector)
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

        if (dataLoader is ISelectionDataLoader<TKey, TValue>)
        {
            var context = (DefaultSelectorBuilder<TValue>)dataLoader.ContextData[typeof(ISelectorBuilder).FullName!]!;
            context.Add(selector);
            return dataLoader;
        }

        var branchKey = selector.ToString();
        return (ISelectionDataLoader<TKey, TValue>)dataLoader.Branch(branchKey, CreateBranch, selector);

        static IDataLoader CreateBranch(
            string key,
            IDataLoader<TKey, TValue> dataLoader,
            Expression<Func<TValue, TValue>> selector)
        {
            var branch =  new SelectionDataLoader<TKey, TValue>(
                (DataLoaderBase<TKey, TValue>)dataLoader,
                key);
            var context = new DefaultSelectorBuilder<TValue>();
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
            _ => new DefaultSelectorBuilder<TValue>());
        context.Add(Rewrite(includeSelector));
        return dataLoader;
    }

    /// <summary>
    /// Applies the selector from the DataLoader state to a queryable.
    /// </summary>
    /// <param name="query">
    /// The queryable to apply the selector to.
    /// </param>
    /// <param name="builder">
    /// The selector builder.
    /// </param>
    /// <param name="key">
    /// The DataLoader key.
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
        ISelectorBuilder builder,
        Expression<Func<T, object?>> key)
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
}

using System.Linq.Expressions;
using System.Reflection;
using GreenDonut.Data.Internal;
using static GreenDonut.Data.Internal.ExpressionHelpers;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Data;

/// <summary>
/// Data loader extensions for projections.
/// </summary>
public static class GreenDonutSelectionDataLoaderExtensions
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
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (selector is null)
        {
            return dataLoader;
        }

        if (dataLoader.ContextData.TryGetValue(DataLoaderStateKeys.Selector, out var value))
        {
            var context = (DefaultSelectorBuilder)value!;
            context.Add(selector);
            return dataLoader;
        }

        var branchKey = selector.ComputeHash();
        var state = new QueryState(DataLoaderStateKeys.Selector, new DefaultSelectorBuilder(selector));
        return (IQueryDataLoader<TKey, TValue>)dataLoader.Branch(branchKey, DataLoaderStateHelper.CreateBranch, state);
    }

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
    public static IDataLoader<TKey, TValue[]> Select<TKey, TValue>(
        this IDataLoader<TKey, TValue[]> dataLoader,
        Expression<Func<TValue, TValue>>? selector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (selector is null)
        {
            return dataLoader;
        }

        if (dataLoader.ContextData.TryGetValue(DataLoaderStateKeys.Selector, out var value))
        {
            var context = (DefaultSelectorBuilder)value!;
            context.Add(selector);
            return dataLoader;
        }

        var branchKey = selector.ComputeHash();
        var state = new QueryState(DataLoaderStateKeys.Selector, new DefaultSelectorBuilder(selector));
        return (IQueryDataLoader<TKey, TValue[]>)dataLoader.Branch(branchKey, DataLoaderStateHelper.CreateBranch, state);
    }

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
    public static IDataLoader<TKey, List<TValue>> Select<TKey, TValue>(
        this IDataLoader<TKey, List<TValue>> dataLoader,
        Expression<Func<TValue, TValue>>? selector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (selector is null)
        {
            return dataLoader;
        }

        if (dataLoader.ContextData.TryGetValue(DataLoaderStateKeys.Selector, out var value))
        {
            var context = (DefaultSelectorBuilder)value!;
            context.Add(selector);
            return dataLoader;
        }

        var branchKey = selector.ComputeHash();
        var state = new QueryState(DataLoaderStateKeys.Selector, new DefaultSelectorBuilder(selector));
        return (IQueryDataLoader<TKey, List<TValue>>)dataLoader.Branch(branchKey, DataLoaderStateHelper.CreateBranch, state);
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
    /// <typeparam name="TResult">
    /// The selector result type.
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
    public static IDataLoader<TKey, TValue> Include<TKey, TValue, TResult>(
        this IDataLoader<TKey, TValue> dataLoader,
        Expression<Func<TValue, TResult>> includeSelector)
        where TKey : notnull
    {
        AssertIncludePossible(dataLoader, includeSelector);

        var context = dataLoader.GetOrSetState(
            DataLoaderStateKeys.Selector,
            _ => new DefaultSelectorBuilder());
        context.Add(Rewrite(includeSelector));
        return dataLoader;
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
    /// <typeparam name="TResult">
    /// The selector result type.
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
    public static IDataLoader<TKey, Page<TValue>> Include<TKey, TValue, TResult>(
        this IDataLoader<TKey, Page<TValue>> dataLoader,
        Expression<Func<TValue, TResult>> includeSelector)
        where TKey : notnull
    {
        AssertIncludePossible(dataLoader, includeSelector);

        var context = dataLoader.GetOrSetState(
            DataLoaderStateKeys.Selector,
            _ => new DefaultSelectorBuilder());
        context.Add(Rewrite(includeSelector));
        return dataLoader;
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
    /// <typeparam name="TResult">
    /// The selector result type.
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
    public static IDataLoader<TKey, List<TValue>> Include<TKey, TValue, TResult>(
        this IDataLoader<TKey, List<TValue>> dataLoader,
        Expression<Func<TValue, TResult>> includeSelector)
        where TKey : notnull
    {
        AssertIncludePossible(dataLoader, includeSelector);

        var context = dataLoader.GetOrSetState(
            DataLoaderStateKeys.Selector,
            _ => new DefaultSelectorBuilder());
        context.Add(Rewrite(includeSelector));
        return dataLoader;
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
    /// <typeparam name="TResult">
    /// The selector result type.
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
    public static IDataLoader<TKey, TValue[]> Include<TKey, TValue, TResult>(
        this IDataLoader<TKey, TValue[]> dataLoader,
        Expression<Func<TValue, TResult>> includeSelector)
        where TKey : notnull
    {
        AssertIncludePossible(dataLoader, includeSelector);

        var context = dataLoader.GetOrSetState(
            DataLoaderStateKeys.Selector,
            _ => new DefaultSelectorBuilder());
        context.Add(Rewrite(includeSelector));
        return dataLoader;
    }

    private static void AssertIncludePossible(IDataLoader dataLoader, Expression includeSelector)
    {
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (!dataLoader.ContextData.ContainsKey(DataLoaderStateKeys.Selector))
        {
            throw new InvalidOperationException(
                "The Include method must be called after the Select method.");
        }

        ArgumentNullException.ThrowIfNull(includeSelector);

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
    }
}

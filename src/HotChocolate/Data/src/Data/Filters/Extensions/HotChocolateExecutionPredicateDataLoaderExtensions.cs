using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Pagination;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Predicates;

/// <summary>
/// Provides extension methods for projection on DataLoader.
/// </summary>
[Experimental(Experiments.Predicates)]
public static class HotChocolateExecutionPredicateDataLoaderExtensions
{
    /// <summary>
    /// Applies a filter context to the data loader.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="context">
    /// The filter context that shall be applied to the data loader.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns a new data loader that applies the selection.
    /// </returns>
    public static IDataLoader<TKey, TValue> Where<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        IFilterContext context)
        where TKey : notnull
    {
        var expression = context.AsPredicate<TValue>();
        return dataLoader.Where(expression);
    }

    /// <summary>
    /// Applies a filter context to the data loader.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="context">
    /// The filter context that shall be applied to the data loader.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns a new data loader that applies the selection.
    /// </returns>
    public static IPagingDataLoader<TKey, Page<TValue>> Where<TKey, TValue>(
        this IPagingDataLoader<TKey, Page<TValue>> dataLoader,
        IFilterContext context)
        where TKey : notnull
    {
        var expression = context.AsPredicate<TValue>();
        return dataLoader.Where(expression);
    }
}

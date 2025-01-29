using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Data;

/// <summary>
/// Provides extension methods for projection on DataLoader.
/// </summary>
public static class HotChocolateExecutionDataLoaderExtensions
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
    public static IDataLoader<TKey, TValue[]> Where<TKey, TValue>(
        this IDataLoader<TKey, TValue[]> dataLoader,
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
    public static IDataLoader<TKey, List<TValue>> Where<TKey, TValue>(
        this IDataLoader<TKey, List<TValue>> dataLoader,
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
    public static IDataLoader<TKey, Page<TValue>> Where<TKey, TValue>(
        this IDataLoader<TKey, Page<TValue>> dataLoader,
        IFilterContext context)
        where TKey : notnull
    {
        var expression = context.AsPredicate<TValue>();
        return dataLoader.Where(expression);
    }

    public static IDataLoader<TKey, TValue> Order<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        ISortingContext context)
        where TKey : notnull
    {
        var definition = context.AsSortDefinition<TValue>();
        return dataLoader.OrderBy(definition);
    }

    public static IDataLoader<TKey, TValue[]> Order<TKey, TValue>(
        this IDataLoader<TKey, TValue[]> dataLoader,
        ISortingContext context)
        where TKey : notnull
    {
        var definition = context.AsSortDefinition<TValue>();
        return dataLoader.OrderBy(definition);
    }

    public static IDataLoader<TKey, List<TValue>> Order<TKey, TValue>(
        this IDataLoader<TKey, List<TValue>> dataLoader,
        ISortingContext context)
        where TKey : notnull
    {
        var definition = context.AsSortDefinition<TValue>();
        return dataLoader.OrderBy(definition);
    }

    public static IDataLoader<TKey, Page<TValue>> Order<TKey, TValue>(
        this IDataLoader<TKey, Page<TValue>> dataLoader,
        ISortingContext context)
        where TKey : notnull
    {
        var definition = context.AsSortDefinition<TValue>();
        return dataLoader.Order(definition);
    }
}

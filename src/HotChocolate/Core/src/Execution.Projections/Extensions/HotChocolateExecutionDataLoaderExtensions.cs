using HotChocolate.Execution;
using HotChocolate.Execution.Processing;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Data;

/// <summary>
/// Provides extension methods for projection on DataLoader.
/// </summary>
public static class HotChocolateExecutionDataLoaderExtensions
{
    /// <summary>
    /// Selects the fields that where selected in the GraphQL selection tree.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="selection">
    /// The selection that shall be applied to the data loader.
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
    public static IDataLoader<TKey, TValue> Select<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        ISelection selection)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dataLoader);
        ArgumentNullException.ThrowIfNull(selection);

        var expression = selection.AsSelector<TValue>();
        return dataLoader.Select(expression);
    }

    public static IDataLoader<TKey, TValue> Select<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        Selection selection)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dataLoader);
        ArgumentNullException.ThrowIfNull(selection);

        var expression = selection.AsSelector<TValue>();
        return dataLoader.Select(expression);
    }

    /// <summary>
    /// Selects the fields that where selected in the GraphQL selection tree.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="selection">
    /// The selection that shall be applied to the data loader.
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
    public static IDataLoader<TKey, TValue[]> Select<TKey, TValue>(
        this IDataLoader<TKey, TValue[]> dataLoader,
        ISelection selection)
        where TKey : notnull
        where TValue : notnull
    {
        var expression = selection.AsSelector<TValue>();
        return dataLoader.Select(expression);
    }

    /// <summary>
    /// Selects the fields that where selected in the GraphQL selection tree.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="selection">
    /// The selection that shall be applied to the data loader.
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
    public static IDataLoader<TKey, List<TValue>> Select<TKey, TValue>(
        this IDataLoader<TKey, List<TValue>> dataLoader,
        ISelection selection)
        where TKey : notnull
        where TValue : notnull
    {
        var expression = selection.AsSelector<TValue>();
        return dataLoader.Select(expression);
    }

    /// <summary>
    /// Selects the fields that where selected in the GraphQL selection tree.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="selection">
    /// The selection that shall be applied to the data loader.
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
    public static IDataLoader<TKey, Page<TValue>> Select<TKey, TValue>(
        this IDataLoader<TKey, Page<TValue>> dataLoader,
        ISelection selection)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dataLoader);
        ArgumentNullException.ThrowIfNull(selection);

        var expression = selection.AsSelector<TValue>();
        return dataLoader.Select(expression);
    }
}

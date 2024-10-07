using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Pagination;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Projections;

/// <summary>
/// Provides extension methods for projection on DataLoader.
/// </summary>
[Experimental(Experiments.Projections)]
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
        if (dataLoader == null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (selection == null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

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
    public static IPagingDataLoader<TKey, Page<TValue>> Select<TKey, TValue>(
        this IPagingDataLoader<TKey, Page<TValue>> dataLoader,
        ISelection selection)
        where TKey : notnull
    {
        if (dataLoader == null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        if (selection == null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        var expression = selection.AsSelector<TValue>();
        dataLoader.Select(expression);
        return dataLoader;
    }
}

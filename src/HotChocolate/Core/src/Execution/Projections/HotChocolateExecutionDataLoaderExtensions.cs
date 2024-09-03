#if NET6_0_OR_GREATER
#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Projections;
using HotChocolate.Pagination;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Projections;

/// <summary>
/// Provides extension methods for projection on DataLoader.
/// </summary>
#if NET8_0_OR_GREATER
[Experimental(Experiments.Projections)]
#endif
public static class HotChocolateExecutionDataLoaderExtensions
{
    private static readonly SelectionExpressionBuilder _builder = new();

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
    public static ISelectionDataLoader<TKey, TValue> Select<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        ISelection selection)
        where TKey : notnull
    {
        var key = $"{dataLoader.GetType().FullName!}.{selection.Id}";
        var expression = selection.DeclaringOperation
            .GetOrAddState<Expression<Func<TValue, TValue>>, SelectionExpressionBuilder>(
                key,
                (_, b) => b.BuildExpression<TValue>(selection),
                _builder);
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
    public static IPagingDataLoader<TKey, TValue> Select<TKey, TValue>(
        this IPagingDataLoader<TKey, TValue> dataLoader,
        ISelection selection)
        where TKey : notnull
    {
        var key = $"{dataLoader.GetType().FullName!}.{selection.Id}";
        var expression = selection.DeclaringOperation
            .GetOrAddState<Expression<Func<TValue, TValue>>, SelectionExpressionBuilder>(
                key,
                (_, b) => b.BuildExpression<TValue>(selection),
                _builder);
        return HotChocolatePaginationBatchingDataLoaderExtensions.Select(dataLoader, expression);
    }
}
#endif

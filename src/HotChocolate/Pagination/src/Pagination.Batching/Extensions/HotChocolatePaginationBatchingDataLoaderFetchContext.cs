using HotChocolate.Pagination;

namespace GreenDonut;

/// <summary>
/// Provides extension method to the <see cref="DataLoaderFetchContext{TValue}"/> for pagination.
/// </summary>
public static class HotChocolatePaginationBatchingDataLoaderFetchContext
{
    /// <summary>
    /// Gets the <see cref="PagingArguments"/> from the DataLoader fetch context.
    /// </summary>
    /// <param name="context">
    /// The DataLoader fetch context.
    /// </param>
    /// <typeparam name="TValue">
    /// The value type of the DataLoader.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="PagingArguments"/> from the DataLoader fetch context.
    /// </returns>
    public static PagingArguments GetPagingArguments<TValue>(
        this DataLoaderFetchContext<TValue> context)
        => context.GetRequiredState<PagingArguments>();
}

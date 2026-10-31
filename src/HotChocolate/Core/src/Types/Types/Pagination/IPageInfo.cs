namespace HotChocolate.Types.Pagination;

/// <summary>
/// Provides basic information about a the page in the data set.
/// </summary>
public interface IPageInfo
{
    /// <summary>
    /// Specifies if the current page has a next page.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token that cancels the operation.
    /// </param>
    ValueTask<bool> HasNextPageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Specifies if the current page has a previous page.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token that cancels the operation.
    /// </param>
    ValueTask<bool> HasPreviousPageAsync(CancellationToken cancellationToken = default);
}

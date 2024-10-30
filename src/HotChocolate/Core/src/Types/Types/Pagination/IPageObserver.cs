#nullable enable
namespace HotChocolate.Types.Pagination;

/// <summary>
/// Allows to inspect a page of a data set.
/// </summary>
public interface IPageObserver
{
    /// <summary>
    /// Is called after the page has been sliced.
    /// </summary>
    /// <param name="items">
    /// The items of the page.
    /// </param>
    /// <param name="pageInfo">
    /// The page information.
    /// </param>
    /// <typeparam name="T">
    /// The item type.
    /// </typeparam>
    void OnAfterSliced<T>(ReadOnlySpan<T> items, IPageInfo pageInfo);
}

#nullable enable

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents a page of a data set.
/// </summary>
public interface IPage
{
    /// <summary>
    /// Gets the items of this page.
    /// </summary>
    IReadOnlyList<object> Items { get; }

    /// <summary>
    /// Gets basic information about this page in the overall data set.
    /// </summary>
    IPageInfo Info { get; }

    /// <summary>
    /// Accepts a page observer and will in turn report the page.
    /// </summary>
    /// <param name="observer">
    /// The page observer.
    /// </param>
    public void Accept(IPageObserver observer);
}

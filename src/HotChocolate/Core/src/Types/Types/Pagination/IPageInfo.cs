#nullable enable

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// Provides basic information about a the page in the data set.
    /// </summary>
    public interface IPageInfo
    {
        /// <summary>
        /// Specifies if the current page has a next page.
        /// </summary>
        bool HasNextPage { get; }

        /// <summary>
        /// Specifies if the current page has a previous page.
        /// </summary>
        bool HasPreviousPage { get; }
    }
}

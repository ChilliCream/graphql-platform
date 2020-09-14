#nullable enable

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// Provides basic information about a the page in the data set.
    /// </summary>
    public interface IPageInfo
    {
        bool HasNextPage { get; }

        bool HasPreviousPage { get; }
    }
}

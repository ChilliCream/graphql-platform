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

        /// <summary>
        /// If <see cref="TotalCount"/> is supported by the <see cref="ICollectionSegmentResolver"/>
        /// then this property will provide the total number of entities the current data set
        /// provides.
        /// </summary>
        long? TotalCount { get; }
    }
}

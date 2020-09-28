namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// Represents the offset paging page info. 
    /// This class provides additional information about the selected page.
    /// </summary>
    public class CollectionSegmentInfo : IPageInfo
    {
        /// <summary>
        /// Initializes <see cref="CollectionSegmentCountType{T}" />.
        /// </summary>
        /// <param name="hasNextPage"></param>
        /// <param name="hasPreviousPage"></param>
        public CollectionSegmentInfo(
            bool hasNextPage,
            bool hasPreviousPage)
        {
            HasNextPage = hasNextPage;
            HasPreviousPage = hasPreviousPage;
        }

        /// <summary>
        /// <c>true</c> if there is another page after the current one.
        /// <c>false</c> if this page is the last page of the current data set / collection.
        /// </summary>
        public bool HasNextPage { get; }

        /// <summary>
        /// <c>true</c> if there is before this page.
        /// <c>false</c> if this page is the first page in the current data set / collection.
        /// </summary>
        public bool HasPreviousPage { get; }
    }
}

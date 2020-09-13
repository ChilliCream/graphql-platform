namespace HotChocolate.Types.Pagination
{
    public class CollectionSegmentInfo : IPageInfo
    {
        public CollectionSegmentInfo(
            bool hasNextPage,
            bool hasPreviousPage,
            long? totalCount = null)
        {
            HasNextPage = hasNextPage;
            HasPreviousPage = hasPreviousPage;
            TotalCount = totalCount;
        }

        public bool HasNextPage { get; }

        public bool HasPreviousPage { get; }

        public long? TotalCount { get; }
    }
}

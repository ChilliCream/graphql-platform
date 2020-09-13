namespace HotChocolate.Types.Pagination
{
    public readonly struct CollectionSegmentInfo : IPageInfo
    {
        public CollectionSegmentInfo(
            bool hasNextPage,
            bool hasPreviousPage,
            int? totalCount = null)
        {
            HasNextPage = hasNextPage;
            HasPreviousPage = hasPreviousPage;
            TotalCount = totalCount;
        }

        public bool HasNextPage { get; }

        public bool HasPreviousPage { get; }

        public int? TotalCount { get; }
    }
}

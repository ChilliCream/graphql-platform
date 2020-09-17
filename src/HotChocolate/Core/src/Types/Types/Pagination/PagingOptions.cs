namespace HotChocolate.Types.Pagination
{
    public struct PagingOptions
    {
        public int? DefaultPageSize { get; set; }

        public int? MaxPageSize { get; set; }

        public bool? IncludeTotalCount { get; set; }
    }
}

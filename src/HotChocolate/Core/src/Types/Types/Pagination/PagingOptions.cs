namespace HotChocolate.Types.Pagination
{
    public struct PagingOptions
    {
        public int? DefaultPageSize { get; set; }

        public int? MaxPageSize { get; set; }

        public bool? IncludeTotalCount { get; set; }

        public bool? Forward { get; set; }

        public bool? Backward { get; set; }
    }
}

namespace HotChocolate.Types.Pagination
{
    public struct PagingSettings
    {
        public int? DefaultPageSize { get; set; }

        public int? MaxPageSize { get; set; }

        public bool? IncludeTotalCount { get; set; }
    }
}

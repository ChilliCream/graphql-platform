namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// The paging options.
    /// </summary>
    public struct PagingOptions
    {
        /// <summary>
        /// Gets or sets the default page size.
        /// </summary>
        public int? DefaultPageSize { get; set; }

        /// <summary>
        /// Gets or sets the max allowed page size.
        /// </summary>
        public int? MaxPageSize { get; set; }

        /// <summary>
        /// Defines if the total count of the paged data set
        /// shall be included into the paging result type.
        /// </summary>
        public bool? IncludeTotalCount { get; set; }
    }
}

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// The offset paging argument values provided by the user.
    /// </summary>
    public readonly struct OffsetPagingArguments
    {
        /// <summary>
        /// Initializes <see cref="OffsetPagingArguments" />.
        /// </summary>
        /// <param name="skip">
        /// The items that shall be skipped.
        /// </param>
        /// <param name="take">
        /// The count of items that shall be included into the page.
        /// </param>
        public OffsetPagingArguments(int? skip, int take)
        {
            Skip = skip;
            Take = take;
        }

        /// <summary>
        /// The items that shall be skipped.
        /// </summary>
        /// <value></value>
        public int? Skip { get; }

        /// <summary>
        /// The count of items that shall be included into the page.
        /// </summary>
        /// <value></value>
        public int Take { get; }
    }
}

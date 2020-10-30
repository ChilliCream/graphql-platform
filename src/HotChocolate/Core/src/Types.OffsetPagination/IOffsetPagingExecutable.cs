namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// If this contract is implemented a <see cref="OffsetPagingHandler"/> will be able to provide
    /// <see cref="OffsetPagingArguments"/> to the executable
    /// </summary>
    public interface IOffsetPagingExecutable
    {
        /// <summary>
        /// Enables or disables offset paging on this executable
        /// </summary>
        /// <param name="options">
        /// Paging options of current context
        /// </param>
        /// <param name="arguments">
        /// Sets the <see cref="OffsetPagingArguments"/> that of the context of this executable
        /// </param>
        /// <param name="includeTotalCount">
        /// Is true when totalCount is included in the selection set
        /// </param>
        IExecutable AddPaging(
            PagingOptions options,
            OffsetPagingArguments? arguments,
            bool includeTotalCount);
    }
}

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// If this contract is implemented a <see cref="CursorPagingHandler"/> will be able to provide
    /// <see cref="CursorPagingArguments"/> to the executable
    /// </summary>
    public interface ICursorPagingExecutable
    {
        /// <summary>
        /// Enables or disables cursor paging on this executable
        /// </summary>
        /// <param name="options">
        /// Paging options of current context
        /// </param>
        /// <param name="arguments">
        /// Sets the <see cref="CursorPagingArguments"/> that of the context of this executable
        /// </param>
        IExecutable AddPaging(
            PagingOptions options,
            CursorPagingArguments? arguments);
    }
}

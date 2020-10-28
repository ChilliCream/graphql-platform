#nullable enable

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// If this contract is implemented <see cref="PagingMiddleware"/> will set
    /// <see cref="ApplyPagingToResultAsync"/> of the <see cref="IPagingHandler"/>
    /// </summary>
    public interface IPagingExecutable
    {
        /// <summary>
        /// Enables or disables offset paging on this executable
        /// </summary>
        /// <param name="handler">
        /// Sets the handler that slices the source data. If the handler is null it will skip paging
        /// </param>
        IExecutable ApplyPaging(ApplyPagingToResultAsync? handler);
    }
}

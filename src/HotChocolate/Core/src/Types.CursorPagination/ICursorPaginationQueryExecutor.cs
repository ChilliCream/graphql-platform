namespace HotChocolate.Types.Pagination;

/// <summary>
/// The query executor is a simplified abstraction that just handles the execution of the sliced query.
/// This allows the paging handler to be more generic without having to sacrifice query optimization.
/// </summary>
/// <typeparam name="TQuery">
/// The type of the query that is sliced.
/// </typeparam>
/// <typeparam name="TEntity">
/// The entity type of the data.
/// </typeparam>
public interface ICursorPaginationQueryExecutor<in TQuery, TEntity> where TQuery : notnull
{
    /// <summary>
    /// Counts the total number of items in the original query
    /// (the query that existed before slicing was applied).
    /// </summary>
    /// <param name="originalQuery">
    /// The original query.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the total number of items in the original query.
    /// </returns>
    ValueTask<int> CountAsync(
        TQuery originalQuery,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes the sliced query and returns the pagination data.
    /// If the total count is requested the executor may rewrite the database query
    /// to get the total count and the sliced items in one go or do separate calls
    /// to the database.
    /// </summary>
    /// <param name="slicedQuery">
    /// The sliced query that should be executed.
    /// </param>
    /// <param name="originalQuery">
    /// The original query.
    /// </param>
    /// <param name="offset">
    /// Teh offset for the index edge.
    /// </param>
    /// <param name="includeTotalCount">
    /// Defines if the total count should be included in the result.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the pagination data.
    /// </returns>
    ValueTask<CursorPaginationData<TEntity>> QueryAsync(
        TQuery slicedQuery,
        TQuery originalQuery,
        int offset,
        bool includeTotalCount,
        CancellationToken cancellationToken);
}

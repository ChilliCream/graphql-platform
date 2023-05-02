namespace HotChocolate.Data.ElasticSearch.Execution;

public interface IElasticSearchExecutable : IExecutable
{
    /// <summary>
    /// Setups a filter operation on query
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    IElasticSearchExecutable WithFiltering(BoolOperation filter);

    /// <summary>
    /// Setups a sort operation on query
    /// </summary>
    /// <param name="sorting"></param>
    /// <returns></returns>
    IElasticSearchExecutable WithSorting(IReadOnlyList<ElasticSearchSortOperation> sorting);
}

public interface IElasticSearchExecutable<T> : IElasticSearchExecutable, IExecutable<T>
{
    /// <summary>
    /// Executes the query and returns results
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<T>> ExecuteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns total count of results
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<int> CountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Setups amount to take of results
    /// </summary>
    /// <param name="take"></param>
    /// <returns></returns>
    IElasticSearchExecutable<T> WithTake(int take);

    /// <summary>
    /// Setups amount to skip before taking results
    /// </summary>
    /// <param name="skip"></param>
    /// <returns></returns>
    IElasticSearchExecutable<T> WithSkip(int skip);
}

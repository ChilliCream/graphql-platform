namespace Mocha.Mediator;

/// <summary>
/// Defines a handler for a query that returns a response.
/// </summary>
/// <typeparam name="TQuery">The type of query to handle.</typeparam>
/// <typeparam name="TResponse">The type of the query result.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Handles the specified query and returns a result.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="ValueTask{TResponse}"/> containing the result.</returns>
    ValueTask<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

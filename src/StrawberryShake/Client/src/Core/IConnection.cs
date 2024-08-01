namespace StrawberryShake;

/// <summary>
/// A connection represents a transport connection to a GraphQL server and allows to execute
/// requests against it.
/// </summary>
/// <typeparam name="TResponseBody"></typeparam>
public interface IConnection<TResponseBody> where TResponseBody : class
{
    /// <summary>
    /// Executes a request and yields the results.
    /// </summary>
    /// <param name="request">
    /// The operation request that shall be send to the server.
    /// </param>
    /// <returns>
    /// The results of the request.
    /// </returns>
    IAsyncEnumerable<Response<TResponseBody>> ExecuteAsync(OperationRequest request);
}

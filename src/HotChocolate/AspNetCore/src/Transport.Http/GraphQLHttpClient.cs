namespace HotChocolate.Transport.Http;

/// <summary>
/// The interface for GraphQL over HTTP client implementations.
/// </summary>
public abstract class GraphQLHttpClient : IDisposable
{
    /// <summary>
    /// Sends the GraphQL request to the specified GraphQL request <see cref="Uri"/>.
    /// </summary>
    /// <param name="request">
    /// The GraphQL over HTTP request.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the HTTP request.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation to send the GraphQL
    /// request to the specified GraphQL request <see cref="Uri"/>.
    /// </returns>
    public abstract Task<GraphQLHttpResponse> SendAsync(
        GraphQLHttpRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    protected virtual void Dispose(bool disposing) { }

    /// <summary>
    /// Creates a new <see cref="GraphQLHttpClient"/> instance.
    /// </summary>
    /// <param name="httpClient">
    /// The underlying HTTP client that is used to send the GraphQL request.
    /// </param>
    /// <param name="disposeHttpClient">
    /// Specifies if <paramref name="httpClient"/> shall be disposed
    /// when the <see cref="GraphQLHttpClient"/> instance is disposed.
    /// </param>
    /// <returns>
    /// Returns the new &lt;see cref="GraphQLHttpClient"/&gt; instance.
    /// </returns>
    public static GraphQLHttpClient Create(HttpClient httpClient, bool disposeHttpClient)
        => new DefaultGraphQLHttpClient(httpClient, disposeHttpClient);
}

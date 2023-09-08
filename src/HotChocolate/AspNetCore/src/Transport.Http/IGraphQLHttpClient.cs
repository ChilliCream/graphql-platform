using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Transport.Http;

/// <summary>
/// The interface for GraphQL over HTTP client implementations.
/// </summary>
public interface IGraphQLHttpClient : IDisposable
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
    public Task<GraphQLHttpResponse> SendAsync(
        GraphQLHttpRequest request,
        CancellationToken cancellationToken = default);
}
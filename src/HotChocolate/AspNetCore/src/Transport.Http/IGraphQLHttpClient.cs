using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Transport.Abstractions;

namespace HotChocolate.Transport.Http;

/// <summary>
/// A GraphQL over http client
/// </summary>
public interface IGraphQLHttpClient
{
    /// <summary>
    /// Sends an <see cref="OperationRequest"/> via GET to the GraphQL server
    /// Only operations of type query are allowed
    /// </summary>
    /// <param name="request">The request to send</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An operation result</returns>
    public Task<OperationResult> ExecuteGetAsync(OperationRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Sends an <see cref="OperationRequest"/> via POST to the GraphQL server
    /// </summary>
    /// <param name="request">The request to send</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An operation result</returns>
    public Task<OperationResult> ExecutePostAsync(OperationRequest request, CancellationToken cancellationToken);
}

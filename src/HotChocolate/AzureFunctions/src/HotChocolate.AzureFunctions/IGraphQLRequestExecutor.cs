using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotChocolate.AzureFunctions;

/// <summary>
/// Represents a GraphQL Over HTTP request executor.
/// </summary>
public interface IGraphQLRequestExecutor
{
    /// <summary>
    /// Executes a GraphQL over HTTP request.
    /// </summary>
    /// <param name="request">
    /// The GraphQL request.
    /// </param>
    /// <returns>
    /// returns the GraphQL HTTP response.
    /// </returns>
    Task<IActionResult> ExecuteAsync(HttpRequest request);
}

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
    /// The HTTP request.
    /// </param>
    /// <returns>
    /// returns the GraphQL HTTP response.
    /// </returns>
    Task<IActionResult> ExecuteAsync(HttpRequest request) => ExecuteAsync(request.HttpContext);

    /// <summary>
    /// Executes a GraphQL over HTTP request.
    /// </summary>
    /// <param name="context">
    /// The HTTP context.
    /// </param>
    /// <returns>
    /// returns the GraphQL HTTP response.
    /// </returns>
    Task<IActionResult> ExecuteAsync(HttpContext context);
}

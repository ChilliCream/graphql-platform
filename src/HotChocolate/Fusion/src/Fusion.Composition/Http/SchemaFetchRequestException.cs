using System.Net;

namespace HotChocolate.Fusion;

/// <summary>
/// The exception that is thrown when a source schema endpoint responds to a schema fetch
/// request with a non-success HTTP status code.
/// </summary>
internal sealed class SchemaFetchRequestException(string message, HttpStatusCode statusCode)
    : InvalidOperationException(message)
{
    /// <summary>
    /// Gets the HTTP status code the source schema endpoint responded with.
    /// </summary>
    public HttpStatusCode StatusCode { get; } = statusCode;
}

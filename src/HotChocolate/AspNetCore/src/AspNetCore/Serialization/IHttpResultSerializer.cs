using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AspNetCore.Serialization;

/// <summary>
/// This interface specifies how a GraphQL result is serialized to a HTTP response.
/// </summary>
public interface IHttpResultSerializer
{
    /// <summary>
    /// Gets the HTTP content type for the specified execution result.
    /// </summary>
    /// <param name="result">
    /// The GraphQL execution result.
    /// </param>
    /// <param name="acceptHeaderValue">
    /// The Accept header value, if provided.
    /// </param>
    /// <returns>
    /// Returns a string representing the content type,
    /// eg. "application/json; charset=utf-8".
    /// </returns>
    string GetContentType(IExecutionResult result, StringValues acceptHeaderValue);

    /// <summary>
    /// Gets the HTTP status code for the specified execution result.
    /// </summary>
    /// <param name="result">
    /// The GraphQL execution result.
    /// </param>
    /// <returns>
    /// Returns the HTTP status code, eg. <see cref="HttpStatusCode.OK"/>.
    /// </returns>
    HttpStatusCode GetStatusCode(IExecutionResult result);

    /// <summary>
    /// Serializes the specified execution result.
    /// </summary>
    /// <param name="result">
    /// The GraphQL execution result.
    /// </param>
    /// <param name="acceptHeaderValue">
    /// The Accept header value, if provided.
    /// </param>
    /// <param name="responseStream">
    /// The HTTP response stream.
    /// </param>
    /// <param name="cancellationToken">
    /// The request cancellation token.
    /// </param>
    ValueTask SerializeAsync(
        IExecutionResult result,
        StringValues acceptHeaderValue,
        Stream responseStream,
        CancellationToken cancellationToken);
}

public interface IHttpRequestInspector
{

}



public interface IHttpResponseFormatter
{
    ValueTask FormatAsync(
        IExecutionResult result,
        StringValues acceptHeaderValue,
        HttpStatusCode? statusCode,
        HttpResponse response,
        CancellationToken cancellationToken);
}

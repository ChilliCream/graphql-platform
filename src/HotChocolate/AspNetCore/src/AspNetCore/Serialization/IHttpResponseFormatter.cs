using System.Net;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Serialization;

/// <summary>
/// This interface specifies how a GraphQL result is formatted to a HTTP response.
/// </summary>
public interface IHttpResponseFormatter
{
    /// <summary>
    /// Inspects the provided accept headers and creates GraphQL request flags
    /// to limit execution behaviors based on the request media types.
    /// </summary>
    /// <param name="acceptMediaTypes">
    /// The media types provided through the accept header.
    /// </param>
    /// <returns>
    /// Returns GraphQL request flags which specifies the allow
    /// execution engine capabilities.
    /// </returns>
    RequestFlags CreateRequestFlags(
        AcceptMediaType[] acceptMediaTypes);

    /// <summary>
    /// Formats the given <paramref name="result"/> into a HTTP <paramref name="response"/>.
    /// </summary>
    /// <param name="response">
    /// The HTTP response.
    /// </param>
    /// <param name="result">
    /// The GraphQL execution result.
    /// </param>
    /// <param name="acceptMediaTypes">
    /// The media types provided through the accept header.
    /// </param>
    /// <param name="proposedStatusCode">
    /// The proposed status code.
    /// </param>
    /// <param name="cancellationToken">
    /// The request cancellation token.
    /// </param>
    ValueTask FormatAsync(
        HttpResponse response,
        IExecutionResult result,
        AcceptMediaType[] acceptMediaTypes,
        HttpStatusCode? proposedStatusCode,
        CancellationToken cancellationToken);

    /// <summary>
    /// Formats the given <paramref name="schema"/> into a GraphQL schema SDL response.
    /// </summary>
    /// <param name="response">
    /// The HTTP response.
    /// </param>
    /// <param name="schema">
    /// The GraphQL schema.
    /// </param>
    /// <param name="version">
    /// The schema version.
    /// </param>
    /// <param name="cancellationToken">
    /// The request cancellation token.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    ValueTask FormatAsync(
        HttpResponse response,
        ISchemaDefinition schema,
        ulong version,
        CancellationToken cancellationToken);
}

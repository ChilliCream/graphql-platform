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
    GraphQLRequestFlags CreateRequestFlags(
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

    ValueTask FormatAsync(
        HttpResponse response,
        ISchema schema,
        ulong version,
        CancellationToken cancellationToken);
}

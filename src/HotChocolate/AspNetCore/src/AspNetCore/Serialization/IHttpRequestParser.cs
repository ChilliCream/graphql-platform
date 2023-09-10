using Microsoft.AspNetCore.Http;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Serialization;

/// <summary>
/// A helper to parse GraphQL HTTP requests.
/// </summary>
public interface IHttpRequestParser
{
    /// <summary>
    /// Parses a JSON GraphQL request from the request body.
    /// </summary>
    /// <param name="requestBody">
    /// A stream representing the HTTP request body.
    /// </param>
    /// <param name="cancellationToken">
    /// The request cancellation token.
    /// </param>
    /// <returns>
    /// Returns the parsed GraphQL request.
    /// </returns>
    ValueTask<IReadOnlyList<GraphQLRequest>> ReadJsonRequestAsync(
        Stream requestBody,
        CancellationToken cancellationToken);

    /// <summary>
    /// Parses a GraphQL HTTP GET request from the HTTP query parameters.
    /// </summary>
    /// <param name="parameters">
    /// The HTTP query parameter collection.
    /// </param>
    /// <returns>
    /// Returns the parsed GraphQL request.
    /// </returns>
    GraphQLRequest ReadParamsRequest(IQueryCollection parameters);

    /// <summary>
    /// Parses the operations string from an GraphQL HTTP MultiPart request.
    /// </summary>
    /// <param name="operations">
    /// The operations string.
    /// </param>
    /// <returns>
    /// Returns the parsed GraphQL request.
    /// </returns>
    IReadOnlyList<GraphQLRequest> ReadOperationsRequest(string operations);
}

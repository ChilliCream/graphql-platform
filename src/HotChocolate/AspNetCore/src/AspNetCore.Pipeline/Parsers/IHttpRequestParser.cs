using System.IO.Pipelines;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Parsers;

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
    /// <param name="skipDocumentBody">
    /// If <c>true</c>, the document body will be skipped during parsing.
    /// </param>
    /// <param name="cancellationToken">
    /// The request cancellation token.
    /// </param>
    /// <returns>
    /// Returns the parsed GraphQL request.
    /// </returns>
    ValueTask<GraphQLRequest[]> ParseRequestAsync(
        PipeReader requestBody,
        bool skipDocumentBody,
        CancellationToken cancellationToken);

    /// <summary>
    /// Parses a JSON GraphQL request from the request body.
    /// </summary>
    /// <param name="documentId">
    /// The operation id.
    /// </param>
    /// <param name="operationName">
    /// The operation name.
    /// </param>
    /// <param name="requestBody">
    /// A stream representing the HTTP request body.
    /// </param>
    /// <param name="skipDocumentBody">
    /// If <c>true</c>, the document body will be skipped during parsing.
    /// </param>
    /// <param name="cancellationToken">
    /// The request cancellation token.
    /// </param>
    /// <returns>
    /// Returns the parsed GraphQL request.
    /// </returns>
    ValueTask<GraphQLRequest> ParsePersistedOperationRequestAsync(
        string documentId,
        string? operationName,
        PipeReader requestBody,
        bool skipDocumentBody,
        CancellationToken cancellationToken);

    /// <summary>
    /// Parses the operations string from an GraphQL HTTP MultiPart request.
    /// </summary>
    /// <param name="sourceText">
    /// The operations string.
    /// </param>
    /// <param name="skipDocumentBody">
    /// If <c>true</c>, the document body will be skipped during parsing.
    /// </param>
    /// <returns>
    /// Returns the parsed GraphQL request.
    /// </returns>
    GraphQLRequest[] ParseRequest(string sourceText, bool skipDocumentBody = false);

    /// <summary>
    /// Parses a GraphQL HTTP GET request from the HTTP query parameters.
    /// </summary>
    /// <param name="parameters">
    /// The HTTP query parameter collection.
    /// </param>
    /// <param name="skipDocumentBody">
    /// If <c>true</c>, the document body will be skipped during parsing.
    /// </param>
    /// <returns>
    /// Returns the parsed GraphQL request.
    /// </returns>
    GraphQLRequest ParseRequestFromParams(IQueryCollection parameters, bool skipDocumentBody = false);

    /// <summary>
    /// Parses the variables and extensions from the HTTP query parameters.
    /// </summary>
    /// <param name="documentId">
    /// The operation id.
    /// </param>
    /// <param name="operationName">
    /// The operation name.
    /// </param>
    /// <param name="parameters">
    /// The HTTP query parameter collection.
    /// </param>
    /// <returns>
    /// Returns the parsed variables and extensions.
    /// </returns>
    GraphQLRequest ParsePersistedOperationRequestFromParams(
        string documentId,
        string? operationName,
        IQueryCollection parameters);
}

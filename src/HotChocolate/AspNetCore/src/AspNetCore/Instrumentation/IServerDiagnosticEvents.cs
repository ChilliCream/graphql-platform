using Microsoft.AspNetCore.Http;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Instrumentation;

/// <summary>
/// The diagnostic events of the GraphQL transport layer.
/// </summary>
public interface IServerDiagnosticEvents
{
    /// <summary>
    /// Called when starting to execute a GraphQL over HTTP request in the transport layer.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    /// <param name="kind">
    /// The HTTP request kind that is being executed.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the execution has finished.
    /// </returns>
    IDisposable ExecuteHttpRequest(HttpContext context, HttpRequestKind kind);

    /// <summary>
    /// Called within the <see cref="ExecuteHttpRequest"/> scope and signals
    /// that a single GraphQL request will be executed.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    /// <param name="request">
    /// The parsed GraphQL request.
    /// </param>
    void StartSingleRequest(HttpContext context, GraphQLRequest request);

    /// <summary>
    /// Called within the <see cref="ExecuteHttpRequest"/> scope and signals
    /// that a GraphQL batch request will be executed.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    /// <param name="batch">
    /// A list of GraphQL requests that represents the batch.
    /// </param>
    void StartBatchRequest(HttpContext context, IReadOnlyList<GraphQLRequest> batch);

    /// <summary>
    /// Called within the <see cref="ExecuteHttpRequest"/> scope and signals
    /// that a GraphQL batch request will be executed.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    /// <param name="request">
    /// A GraphQL request with multiple operations in a single GraphQL document.
    /// </param>
    /// <param name="operations">
    /// A list of operation names that represents the execution order of the
    /// operations within the GraphQL document.
    /// </param>
    void StartOperationBatchRequest(
        HttpContext context,
        GraphQLRequest request,
        IReadOnlyList<string> operations);

    /// <summary>
    /// Called within the <see cref="ExecuteHttpRequest"/> scope and signals
    /// that a error occurred while processing the GraphQL over HTTP request.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    /// <param name="error">
    /// The error.
    /// </param>
    void HttpRequestError(HttpContext context, IError error);

    /// <summary>
    /// Called within the <see cref="ExecuteHttpRequest"/> scope and signals
    /// that a error occurred while processing the GraphQL over HTTP request.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    /// <param name="exception">
    /// The <see cref="Exception"/>.
    /// </param>
    void HttpRequestError(HttpContext context, Exception exception);

    /// <summary>
    /// Called when starting to parse a GraphQL HTTP request.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the parsing is completed.
    /// </returns>
    IDisposable ParseHttpRequest(HttpContext context);

    /// <summary>
    /// Called within the <see cref="ParseHttpRequest"/> scope and signals
    /// that a error occurred while parsing the GraphQL request.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    /// <param name="errors">
    /// The errors.
    /// </param>
    void ParserErrors(HttpContext context, IReadOnlyList<IError> errors);

    /// <summary>
    /// Called when starting to format a GraphQL query result.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    /// <param name="result">
    /// The query result.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when GraphQL query result is written to the response stream.
    /// </returns>
    IDisposable FormatHttpResponse(HttpContext context, IOperationResult result);

    /// <summary>
    /// Called when starting to establish a GraphQL WebSocket session.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    /// <returns>
    /// A scope that will enclose the whole WebSocket session and is disposed when the
    /// session is closed.
    /// </returns>
    IDisposable WebSocketSession(HttpContext context);

    /// <summary>
    /// Called within the <see cref="WebSocketSession"/> scope and signals
    /// that a error occurred that terminated the session.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    /// <param name="exception">
    /// The <see cref="Exception"/>.
    /// </param>
    void WebSocketSessionError(HttpContext context, Exception exception);
}

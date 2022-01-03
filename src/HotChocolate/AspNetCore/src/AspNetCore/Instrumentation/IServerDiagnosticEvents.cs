using System;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Execution.Instrumentation;

public interface IServerDiagnosticEvents
{
    /// <summary>
    /// Called when starting to execute a GraphQL request in the transport layer.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <returns>
    /// A scope that will be disposed when the execution has finished.
    /// </returns>
    IDisposable ExecuteHttpRequest(HttpContext context);

    void HttpRequestCancellation(HttpContext context);

    /// <summary>
    /// Called at the end of the execution if an exception occurred at some point,
    /// including unhandled exceptions when resolving fields.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <param name="exception">
    /// The last exception that occurred.
    /// </param>
    void HttpRequestError(HttpContext context, Exception exception);

    /// <summary>
    /// Called at the end of the execution if an exception occurred at some point,
    /// including unhandled exceptions when resolving fields.
    /// </summary>
    /// <param name="context">
    /// The request context encapsulates all GraphQL-specific information about an
    /// individual GraphQL request.
    /// </param>
    /// <param name="exception">
    /// The last exception that occurred.
    /// </param>
    void HttpRequestError(HttpContext context, IError exception);

    IDisposable ParseHttpRequest(HttpContext context);

    IDisposable FormatHttpResult(HttpContext context);
}

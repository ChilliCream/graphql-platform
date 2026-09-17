using Microsoft.AspNetCore.Http;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

/// <summary>
/// Answers a request that no GraphQL middleware handled. From the 2026-09-03 revision of the
/// GraphQL over HTTP specification on, a request on the GraphQL endpoint whose method the
/// endpoint does not support is answered 405 with an <c>Allow</c> header, an OPTIONS request
/// is answered 204 with the same header, and a POST request whose Content-Type the endpoint
/// does not support is answered 415. Every other request is answered 404.
/// </summary>
public sealed class HttpUnsupportedRequestMiddleware : MiddlewareBase
{
    private static readonly string s_allowWithGet = string.Join(
        ", ",
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Post);

    private static readonly string s_allowWithoutGet =
        string.Join(", ", HttpMethods.Options, HttpMethods.Post);

    private readonly PathString? _path;

    /// <summary>
    /// Creates a new instance of <see cref="HttpUnsupportedRequestMiddleware"/>.
    /// </summary>
    /// <param name="next">
    /// The next middleware in line.
    /// </param>
    /// <param name="executor">
    /// The request executor proxy.
    /// </param>
    /// <param name="baseOptions">
    /// The GraphQL server options.
    /// </param>
    /// <param name="path">
    /// The path of the GraphQL endpoint, or <c>null</c> when every request that reaches this
    /// middleware is on the endpoint.
    /// </param>
    public HttpUnsupportedRequestMiddleware(
        RequestDelegate next,
        HttpRequestExecutorProxy executor,
        GraphQLServerOptions baseOptions,
        PathString? path)
        : base(next, executor, baseOptions)
    {
        _path = path;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsGraphQLEndpoint(context.Request))
        {
            var options = GetOptions(context);

            // a GET or HEAD the endpoint supports but no middleware handled carries no GraphQL
            // request, which is not a refusal of the method.
            if (options.EnableGetRequests && context.Request.IsGetOrHeadMethod())
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var session = await Executor.GetOrCreateSessionAsync(context.RequestAborted);

            if (session.ReportsUnsupportedMethodOrMediaType)
            {
                var allow = options.EnableGetRequests ? s_allowWithGet : s_allowWithoutGet;

                // a POST that no middleware handled carries a Content-Type the endpoint does
                // not support.
                if (HttpMethods.IsPost(context.Request.Method))
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    return;
                }

                // RFC 9110, section 9.3.7: OPTIONS asks for the methods the target resource
                // supports.
                if (HttpMethods.IsOptions(context.Request.Method))
                {
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    context.Response.Headers.Allow = allow;
                    return;
                }

                // RFC 9110, section 15.5.6 requires a 405 to list the methods the target
                // resource supports.
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                context.Response.Headers.Allow = allow;
                return;
            }
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    private bool IsGraphQLEndpoint(HttpRequest request)
    {
        if (_path is not { } path)
        {
            return true;
        }

        var isBelowPath = request.Path.StartsWithSegments(
            path,
            StringComparison.OrdinalIgnoreCase,
            out var remaining);

        return isBelowPath && remaining.Value is null or "" or "/";
    }
}

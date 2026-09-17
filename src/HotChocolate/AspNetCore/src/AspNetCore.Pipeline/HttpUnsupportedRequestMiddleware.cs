using Microsoft.AspNetCore.Http;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

/// <summary>
/// Answers a request that no GraphQL middleware handled. From the 2026-09-03 revision of the
/// GraphQL over HTTP specification on, a request on the GraphQL endpoint whose method the
/// endpoint does not support is answered 405 with an <c>Allow</c> header, and a POST request
/// whose Content-Type the endpoint does not support is answered 415. Every other request is
/// answered 404.
/// </summary>
public sealed class HttpUnsupportedRequestMiddleware : MiddlewareBase
{
    private static readonly string s_allowGetHeadPost =
        string.Join(", ", HttpMethods.Get, HttpMethods.Head, HttpMethods.Post);

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
            var session = await Executor.GetOrCreateSessionAsync(context.RequestAborted);

            if (session.ReportsUnsupportedMethodOrMediaType)
            {
                var options = GetOptions(context);

                // a POST that no middleware handled carries a Content-Type the endpoint does
                // not support.
                if (HttpMethods.IsPost(context.Request.Method))
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    return;
                }

                // RFC 9110, section 15.5.6 requires a 405 to list the methods the target
                // resource supports.
                if (!(options.EnableGetRequests && context.Request.IsGetOrHeadMethod()))
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    context.Response.Headers.Allow = options.EnableGetRequests
                        ? s_allowGetHeadPost
                        : HttpMethods.Post;
                    return;
                }
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

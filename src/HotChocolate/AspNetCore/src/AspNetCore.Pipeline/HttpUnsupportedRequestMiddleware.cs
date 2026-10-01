using Microsoft.AspNetCore.Http;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

/// <summary>
/// Answers a request that no GraphQL middleware handled. From the 2026-09-03 revision of the
/// GraphQL over HTTP specification on, a request on the GraphQL endpoint whose method the
/// endpoint does not support is answered 405 with an <c>Allow</c> header, an OPTIONS request
/// is answered 204 with the same header, and a POST or QUERY request whose Content-Type the
/// endpoint does not support is answered 415. When QUERY requests are enabled, those responses
/// also carry an <c>Accept-Query</c> header. Every other request is answered 404.
/// </summary>
public sealed class HttpUnsupportedRequestMiddleware : MiddlewareBase
{
    private readonly PathString? _path;
    private string? _allow;

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
                // a POST, or a QUERY while QUERY is enabled, that no middleware handled carries a
                // Content-Type the endpoint does not support.
                if (HttpMethods.IsPost(context.Request.Method)
                    || (options.EnableQueryRequests && HttpMethods.IsQuery(context.Request.Method)))
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    WriteAcceptQuery(context.Response, options);
                    return;
                }

                // RFC 9110, section 9.3.7: OPTIONS asks for the methods the target resource
                // supports.
                if (HttpMethods.IsOptions(context.Request.Method))
                {
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    context.Response.Headers.Allow = GetAllow(options);
                    WriteAcceptQuery(context.Response, options);
                    return;
                }

                // RFC 9110, section 15.5.6 requires a 405 to list the methods the target
                // resource supports.
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                context.Response.Headers.Allow = GetAllow(options);
                WriteAcceptQuery(context.Response, options);
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

    // The Allow header for the options this middleware serves, built on first use.
    private string GetAllow(GraphQLServerOptions options)
        => _allow ??= BuildAllow(options);

    private static string BuildAllow(GraphQLServerOptions options)
    {
        var methods = new List<string>(5);

        if (options.EnableGetRequests)
        {
            methods.Add(HttpMethods.Get);
            methods.Add(HttpMethods.Head);
        }

        methods.Add(HttpMethods.Options);
        methods.Add(HttpMethods.Post);

        if (options.EnableQueryRequests)
        {
            methods.Add(HttpMethods.Query);
        }

        return string.Join(", ", methods);
    }

    // RFC 10008, section 3: Accept-Query advertises the QUERY method and the media types its
    // body may use.
    private static void WriteAcceptQuery(HttpResponse response, GraphQLServerOptions options)
    {
        if (options.EnableQueryRequests)
        {
            response.Headers[HttpHeaderKeys.AcceptQuery] = HttpHeaderValues.AcceptQueryMediaTypes;
        }
    }
}

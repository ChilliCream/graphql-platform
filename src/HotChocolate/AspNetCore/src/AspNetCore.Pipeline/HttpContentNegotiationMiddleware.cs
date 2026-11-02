using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

/// <summary>
/// Declares <c>Accept</c> as a selecting header, through <c>Vary</c>, on every request whose
/// response the GraphQL endpoint selects by its <c>Accept</c> header: a GET, HEAD, POST, or QUERY
/// on the endpoint.
/// </summary>
/// <param name="next">
/// The next middleware in line.
/// </param>
/// <param name="path">
/// The path of the GraphQL endpoint, or <c>null</c> when every request that reaches this
/// middleware is on the endpoint.
/// </param>
internal sealed class HttpContentNegotiationMiddleware(RequestDelegate next, PathString? path)
{
    public Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        if ((path is not { } endpointPath || request.IsEndpointPath(endpointPath))
            && (HttpMethods.IsGet(request.Method)
                || HttpMethods.IsHead(request.Method)
                || HttpMethods.IsPost(request.Method)
                || HttpMethods.IsQuery(request.Method)))
        {
            context.Response.Headers.AppendVary(HeaderNames.Accept);
        }

        return next(context);
    }
}

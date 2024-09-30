using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Authorization;

public sealed class DefaultQueryRequestFactory : IOpaQueryRequestFactory
{
    public OpaQueryRequest CreateRequest(AuthorizationContext context, AuthorizeDirective directive)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (directive is null)
        {
            throw new ArgumentNullException(nameof(directive));
        }

        var httpContext = (HttpContext)context.ContextData[nameof(HttpContext)]!;
        var connection = httpContext.Connection;

        var policy = new Policy(
            directive.Policy ?? string.Empty,
            directive.Roles ?? Array.Empty<string>());

        var originalRequest = new OriginalRequest(
            httpContext.Request.Headers,
            httpContext.Request.Host.Value ?? string.Empty,
            httpContext.Request.Method,
            httpContext.Request.Path.Value!,
            httpContext.Request.Query,
            httpContext.Request.Scheme);

        var source = new IPAndPort(
            connection.RemoteIpAddress!.ToString(),
            connection.RemotePort);

        var destination = new IPAndPort(
            connection.LocalIpAddress!.ToString(),
            connection.LocalPort);

        return new OpaQueryRequest(policy, originalRequest, source, destination);
    }
}

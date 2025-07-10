using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// Default implementation of <see cref="IOpaQueryRequestFactory"/>.
/// </summary>
internal sealed class DefaultQueryRequestFactory : IOpaQueryRequestFactory
{
    private readonly OpaOptions _options;

    public DefaultQueryRequestFactory(IOptions<OpaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    /// <inheritdoc/>
    public OpaQueryRequest CreateRequest(OpaAuthorizationHandlerContext context, AuthorizeDirective directive)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(directive);

        var httpContext =
            context.Resource switch
            {
                IMiddlewareContext middlewareContext =>
                    (HttpContext)middlewareContext.ContextData[nameof(HttpContext)]!,
                AuthorizationContext authorizationContext =>
                    (HttpContext)authorizationContext.ContextData[nameof(HttpContext)]!,
                _ => throw new ArgumentException("Invalid context data.")
            };
        var connection = httpContext.Connection;

        var policy = new Policy(
            directive.Policy ?? string.Empty,
            directive.Roles ?? Array.Empty<string>());

        var originalRequest = new OriginalRequest(
            httpContext.Request.Headers,
#if NET8_0
            httpContext.Request.Host.Value,
#else
            httpContext.Request.Host.Value ?? string.Empty,
#endif
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

        object? extensions = null;
        if (directive.Policy is not null &&
            _options.GetOpaQueryRequestExtensionsHandler(directive.Policy) is { } extensionsHandler)
        {
            extensions = extensionsHandler(context);
        }

        return new OpaQueryRequest(policy, originalRequest, source, destination, extensions);
    }
}

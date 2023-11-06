using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

internal static class GlobalStateHelpers
{
    public static HttpContext GetHttpContext(IPureResolverContext context)
    {
        if (context.ContextData.TryGetValue(nameof(HttpContext), out var value) &&
            value is HttpContext httpContext)
        {
            return httpContext;
        }

        throw new MissingStateException("Resolver", nameof(HttpContext), StateKind.Global);
    }

    public static HttpRequest GetHttpRequest(IPureResolverContext context)
        => GetHttpContext(context).Request;

    public static HttpResponse GetHttpResponse(IPureResolverContext context)
        => GetHttpContext(context).Response;
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AspNetCore;

internal static class HttpContextExtensions
{
    public static GraphQLServerOptions? GetGraphQLServerOptions(this HttpContext context)
        => context.GetEndpoint()?.Metadata.GetMetadata<GraphQLServerOptions>() ??
           (context.Items.TryGetValue(nameof(GraphQLServerOptions), out var o) &&
            o is GraphQLServerOptions options
                ? options
                : null);
    
    public static bool IsTracingEnabled(this HttpContext context)
    {
        IHeaderDictionary headers = context.Request.Headers;

        return (headers.TryGetValue(HttpHeaderKeys.Tracing, out StringValues values)
                || headers.TryGetValue(HttpHeaderKeys.ApolloTracing, out values)) &&
               values.Any(v => v == HttpHeaderValues.TracingEnabled);
    }

    public static bool IncludeQueryPlan(this HttpContext context)
    {
        IHeaderDictionary headers = context.Request.Headers;

        if (headers.TryGetValue(HttpHeaderKeys.QueryPlan, out StringValues values) &&
            values.Any(v => v == HttpHeaderValues.IncludeQueryPlan))
        {
            return true;
        }

        return false;
    }
}

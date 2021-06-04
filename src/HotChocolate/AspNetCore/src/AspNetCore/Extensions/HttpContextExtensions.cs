using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AspNetCore
{
    internal static class HttpContextExtensions
    {
        public static GraphQLServerOptions? GetGraphQLServerOptions(this HttpContext context) =>
            context.GetEndpoint()?.Metadata.GetMetadata<GraphQLServerOptions>();

        public static GraphQLToolOptions? GetGraphQLToolOptions(this HttpContext context) =>
            GetGraphQLServerOptions(context)?.Tool;

        public static bool IsTracingEnabled(this HttpContext context)
        {
            IHeaderDictionary headers = context.Request.Headers;

            if ((headers.TryGetValue(HttpHeaderKeys.Tracing, out StringValues values)
                 || headers.TryGetValue(HttpHeaderKeys.ApolloTracing, out values)) &&
                values.Any(v => v == HttpHeaderValues.TracingEnabled))
            {
                return true;
            }

            return false;
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
}

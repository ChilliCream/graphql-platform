using System.Linq;
using HotChocolate.AspNetCore.Subscriptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AspNetCore.Utilities
{
    public static class HttpContextExtensions
    {
        public static bool IsTracingEnabled(this HttpContext context)
        {
            IHeaderDictionary headers = context.Request.Headers;

            if ((headers.TryGetValue(HttpHeaderKeys.Tracing, out StringValues values)
                || headers.TryGetValue(HttpHeaderKeys.ApolloTracing, out values))
                && values.Any(v => v == HttpHeaderValues.TracingEnabled))
            {
                return true;
            }

            return false;
        }
    }
}

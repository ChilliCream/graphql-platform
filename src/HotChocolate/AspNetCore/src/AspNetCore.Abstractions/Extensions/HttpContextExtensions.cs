using System;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore
{
    public static class HttpContextExtensions
    {
        public static CancellationToken GetCancellationToken(
            this HttpContext context)
        {
            return context.RequestAborted;
        }

        public static bool IsTracingEnabled(this HttpContext context)
        {
            return context.Request.Headers
                .TryGetValue(HttpHeaderKeys.Tracing,
                    out StringValues values) &&
                        values.Any(v => v == HttpHeaderValues.TracingEnabled);
        }
    }
}

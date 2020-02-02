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

        public static IPrincipal GetUser(this HttpContext context)
        {
            return context.User;
        }

        public static bool IsTracingEnabled(this HttpContext context)
        {
            return context.Request.Headers
                .TryGetValue(HttpHeaderKeys.Tracing,
                    out StringValues values) &&
                        values.Any(v => v == HttpHeaderValues.TracingEnabled);
        }

        public static bool IsValidPath(
            this HttpContext context,
            PathString path)
        {
            return context.Request.Path.Equals(path,
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsValidPath(
            this HttpContext context,
            PathString path1, PathString path2)
        {
            return context.Request.Path.Equals(path1,
                StringComparison.OrdinalIgnoreCase)
                || context.Request.Path.Equals(path2,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}

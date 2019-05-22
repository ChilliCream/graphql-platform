using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
#else
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    internal static class HttpContextExtensions
    {
#if ASPNETCLASSIC
        public static IServiceProvider CreateRequestServices(
            this HttpContext context,
            IServiceProvider services)
        {
            context.Environment.Add(EnvironmentKeys.ServiceProvider,
                services);

            return services;
        }
#endif

        public static CancellationToken GetCancellationToken(
            this HttpContext context)
        {
#if ASPNETCLASSIC
            return context.Request.CallCancelled;
#else
            return context.RequestAborted;
#endif
        }

        public static IPrincipal GetUser(this HttpContext context)
        {
#if ASPNETCLASSIC
            return context.Request.User;
#else
            return context.User;
#endif
        }

        public static bool IsTracingEnabled(this HttpContext context)
        {
#if ASPNETCLASSIC
            return context.Request.Headers
                .TryGetValue(HttpHeaderKeys.Tracing,
                    out string[] values) &&
                        values.Any(v => v == HttpHeaderValues.TracingEnabled);
#else
            return context.Request.Headers
                .TryGetValue(HttpHeaderKeys.Tracing,
                    out StringValues values) &&
                        values.Any(v => v == HttpHeaderValues.TracingEnabled);
#endif
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

        public static bool IsValidPath(
            this HttpContext context,
            params PathString[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if(!IsValidPath(context, paths[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

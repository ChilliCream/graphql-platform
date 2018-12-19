using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
#else
using Microsoft.AspNetCore.Http;
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
            IServiceProvider rootServiceProvider)
        {
            var services = new Dictionary<Type, object>
            {
                { typeof(HttpContext), context }
            };
            var serviceProvider = new RequestServiceProvider(
                rootServiceProvider,
                services);

            context.Environment.Add(EnvironmentKeys.ServiceProvider,
                serviceProvider);

            return serviceProvider;
        }
#else

        public static IServiceProvider CreateRequestServices(
            this HttpContext context)
        {
            var services = new Dictionary<Type, object>
            {
                { typeof(HttpContext), context }
            };

            return new RequestServiceProvider(
                context.RequestServices, services);
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

        public static bool IsValidPath(
            this HttpContext context,
            PathString path)
        {
            return context.Request.Path.StartsWithSegments(path);
        }
    }
}

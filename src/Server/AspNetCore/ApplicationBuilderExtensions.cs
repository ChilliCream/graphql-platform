using System;

#if ASPNETCLASSIC
using HotChocolate.Execution;
using Microsoft.Owin;
using Owin;
using IApplicationBuilder = Owin.IAppBuilder;
#else
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public static class ApplicationBuilderExtensions
    {
#if ASPNETCLASSIC
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder,
            IServiceProvider serviceProvider)
        {
            return applicationBuilder
                .UseGraphQL(serviceProvider, new QueryMiddlewareOptions());
        }
#else
        
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder
                .UseGraphQL(new QueryMiddlewareOptions());
        }
#endif

#if ASPNETCLASSIC
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder,
            IServiceProvider serviceProvider,
            PathString path)
        {
            var options = new QueryMiddlewareOptions
            {
                Path = path.HasValue ? path : new PathString("/")
            };

            return applicationBuilder
                .UseGraphQL(serviceProvider, options);
        }
#else
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder,
            PathString path)
        {
            var options = new QueryMiddlewareOptions
            {
                Path = path.HasValue ? path : new PathString("/")
            };

            return applicationBuilder
                .UseGraphQL(options);
        }
#endif

#if ASPNETCLASSIC
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder,
            IServiceProvider serviceProvider,
            QueryMiddlewareOptions options)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var executer = (IQueryExecuter)serviceProvider
                .GetService(typeof(IQueryExecuter));

            return applicationBuilder
                .Use<PostQueryMiddleware>(executer, options)
                .Use<GetQueryMiddleware>(executer, options);
                //.Use<SubscriptionMiddleware>(executer, options);
        }
#else
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder,
            QueryMiddlewareOptions options)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return applicationBuilder
                .UseMiddleware<PostQueryMiddleware>(options)
                .UseMiddleware<GetQueryMiddleware>(options)
                .UseMiddleware<SubscriptionMiddleware>(options);
        }
#endif
    }
}

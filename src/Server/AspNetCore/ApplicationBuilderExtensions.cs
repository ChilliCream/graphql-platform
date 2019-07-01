using System;

#if ASPNETCLASSIC
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
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

            IQueryExecutor executor = serviceProvider
                .GetService<IQueryExecutor>();

            IQueryResultSerializer serializer = serviceProvider
                .GetService<IQueryResultSerializer>()
                ?? new JsonQueryResultSerializer();

            IDocumentCache cache = serviceProvider
                .GetRequiredService<IDocumentCache>();

            IDocumentHashProvider hashProvider = serviceProvider
                .GetRequiredService<IDocumentHashProvider>();

            return applicationBuilder
                .Use<PostQueryMiddleware>(executor, serializer, cache, hashProvider, options)
                .Use<GetQueryMiddleware>(executor, serializer, options)
                //.Use<SubscriptionMiddleware>(executor, options)
                .Use<SchemaMiddleware>(executor, options);
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
                .UseMiddleware<SubscriptionMiddleware>(options)
                .UseMiddleware<SchemaMiddleware>(options);
        }
#endif
    }
}

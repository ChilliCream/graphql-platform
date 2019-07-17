using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Owin;
using IApplicationBuilder = Owin.IAppBuilder;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.AspNetClassic
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder applicationBuilder,
            IServiceProvider serviceProvider)
        {
            return applicationBuilder
                .UseGraphQL(serviceProvider, new QueryMiddlewareOptions());
        }

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
                .Use<SchemaMiddleware>(executor, options);
        }
    }
}

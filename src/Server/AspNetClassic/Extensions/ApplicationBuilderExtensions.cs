using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Owin;
using IApplicationBuilder = Owin.IAppBuilder;
using HotChocolate.Execution;
using HotChocolate.Execution.Batching;
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
                .GetRequiredService<IQueryExecutor>();

            IBatchQueryExecutor batchExecutor = serviceProvider
                .GetRequiredService<IBatchQueryExecutor>();

            IQueryResultSerializer serializer = serviceProvider
                .GetService<IQueryResultSerializer>()
                ?? new JsonQueryResultSerializer();

            IDocumentCache cache = serviceProvider
                .GetRequiredService<IDocumentCache>();

            IDocumentHashProvider hashProvider = serviceProvider
                .GetRequiredService<IDocumentHashProvider>();

            OwinContextAccessor contextAccessor =
                serviceProvider.GetService<OwinContextAccessor>();

            return applicationBuilder
                .Use<PostQueryMiddleware>(
                    executor, batchExecutor, serializer,
                    cache, hashProvider, contextAccessor,
                    options)
                .Use<GetQueryMiddleware>(
                    executor, serializer, contextAccessor,
                    options)
                .Use<SchemaMiddleware>(executor, options);
        }
    }
}

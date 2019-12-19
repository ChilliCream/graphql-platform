using System;
using Microsoft.Extensions.DependencyInjection;
using Owin;
using IApplicationBuilder = Owin.IAppBuilder;
using HotChocolate.Execution;
using HotChocolate.Execution.Batching;
using HotChocolate.Language;

namespace HotChocolate.AspNetClassic
{
    public static class HttpPostApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQLHttpPost(
            this IApplicationBuilder applicationBuilder,
            IServiceProvider serviceProvider) =>
            UseGraphQLHttpPost(
                applicationBuilder,
                serviceProvider,
                new HttpPostMiddlewareOptions());

        public static IApplicationBuilder UseGraphQLHttpPost(
            this IApplicationBuilder applicationBuilder,
            IServiceProvider serviceProvider,
            IHttpPostMiddlewareOptions options)
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

            IBatchQueryExecutor batchQueryExecutor = serviceProvider
                .GetRequiredService<IBatchQueryExecutor>();

            IQueryResultSerializer resultSerializer = serviceProvider
                .GetRequiredService<IQueryResultSerializer>();

            IResponseStreamSerializer streamSerializer = serviceProvider
                .GetRequiredService<IResponseStreamSerializer>();

            IDocumentCache documentCache = serviceProvider
                .GetRequiredService<IDocumentCache>();

            IDocumentHashProvider documentHashProvider = serviceProvider
                .GetRequiredService<IDocumentHashProvider>();

            IErrorHandler errorHandler = serviceProvider
                .GetRequiredService<IErrorHandler>();

            OwinContextAccessor contextAccessor =
                serviceProvider.GetService<OwinContextAccessor>();

            return applicationBuilder.Use<HttpPostMiddleware>(
                options, contextAccessor,
                executor, batchQueryExecutor,
                resultSerializer, streamSerializer,
                documentCache, documentHashProvider,
                errorHandler);
        }
    }
}

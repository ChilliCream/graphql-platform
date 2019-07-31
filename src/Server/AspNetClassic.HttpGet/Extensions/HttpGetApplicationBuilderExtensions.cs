using System;
using Microsoft.Extensions.DependencyInjection;
using Owin;
using IApplicationBuilder = Owin.IAppBuilder;
using HotChocolate.Execution;

namespace HotChocolate.AspNetClassic
{
    public static class HttpGetSchemaHttpGetApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQLHttpGet(
            this IApplicationBuilder applicationBuilder,
            IServiceProvider serviceProvider) =>
            UseGraphQLHttpGet(
                applicationBuilder,
                serviceProvider,
                new HttpGetMiddlewareOptions());

        public static IApplicationBuilder UseGraphQLHttpGet(
            this IApplicationBuilder applicationBuilder,
            IServiceProvider serviceProvider,
            IHttpGetMiddlewareOptions options)
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

            IQueryResultSerializer serializer = serviceProvider
                .GetRequiredService<IQueryResultSerializer>();

            OwinContextAccessor contextAccessor =
                serviceProvider.GetService<OwinContextAccessor>();

            return applicationBuilder.Use<HttpGetMiddleware>(
                options, contextAccessor, executor, serializer);
        }
    }
}

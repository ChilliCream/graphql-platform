using System;
using Microsoft.Extensions.DependencyInjection;
using Owin;
using IApplicationBuilder = Owin.IAppBuilder;
using HotChocolate.Execution;

namespace HotChocolate.AspNetClassic
{
    public static class HttpGetSchemaApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGraphQLHttpGetSchema(
            this IApplicationBuilder applicationBuilder,
            IServiceProvider serviceProvider) =>
            UseGraphQLHttpGetSchema(
                applicationBuilder,
                serviceProvider,
                new HttpGetSchemaMiddlewareOptions());

        public static IApplicationBuilder UseGraphQLHttpGetSchema(
            this IApplicationBuilder applicationBuilder,
            IServiceProvider serviceProvider,
            IHttpGetSchemaMiddlewareOptions options)
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

            return applicationBuilder.Use<HttpGetSchemaMiddleware>(
                options, executor);
        }     
    }
}

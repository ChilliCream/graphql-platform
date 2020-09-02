using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateAspNetCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLServerCore(
            this IServiceCollection services,
            int maxAllowedRequestSize = 20 * 1000 * 1000)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddGraphQLCore();
            services.TryAddSingleton<IHttpResultSerializer, DefaultHttpResultSerializer>();
            services.TryAddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>();
            services.TryAddSingleton<IRequestParser>(
                sp => new DefaultRequestParser(
                    sp.GetRequiredService<IDocumentCache>(),
                    sp.GetRequiredService<IDocumentHashProvider>(),
                    maxAllowedRequestSize,
                    sp.GetRequiredService<ParserOptions>()));
            return services;
        }

        public static IRequestExecutorBuilder AddGraphQLServer(
            this IServiceCollection services,
            NameString schemaName = default,
            int maxAllowedRequestSize = 20 * 1000 * 1000) =>
            services
                .AddGraphQLServerCore(maxAllowedRequestSize)
                .AddGraphQL(schemaName);

        public static IRequestExecutorBuilder AddGraphQLServer(
            this IRequestExecutorBuilder builder,
            NameString schemaName = default) =>
            builder.Services.AddGraphQLServer(schemaName);
    }
}

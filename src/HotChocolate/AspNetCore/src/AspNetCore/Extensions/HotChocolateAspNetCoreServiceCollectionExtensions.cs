using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HotChocolateAspNetCoreServiceCollectionExtensions
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
            services.TryAddSingleton<IHttpResultSerializer>(
                new DefaultHttpResultSerializer());
            services.TryAddSingleton<IHttpRequestParser>(
                sp => new DefaultHttpRequestParser(
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
                .AddGraphQL(schemaName)
                .AddHttpRequestInterceptor()
                .AddSubscriptionServices();

        public static IRequestExecutorBuilder AddGraphQLServer(
            this IRequestExecutorBuilder builder,
            NameString schemaName = default) =>
            builder.Services.AddGraphQLServer(schemaName);

        [Obsolete(
            "Use the new configuration API -> " +
            "services.AddGraphQLServer().AddQueryType<Query>()...")]
        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            ISchema schema,
            int maxAllowedRequestSize = 20 * 1000 * 1000) =>
            RequestExecutorBuilderLegacyHelper.SetSchema(
                services
                    .AddGraphQLServerCore(maxAllowedRequestSize)
                    .AddGraphQL()
                    .AddHttpRequestInterceptor()
                    .AddSubscriptionServices(),
                schema)
                .Services;

        [Obsolete(
            "Use the new configuration API -> " +
            "services.AddGraphQLServer().AddQueryType<Query>()...")]
        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            Func<IServiceProvider, ISchema> schemaFactory,
            int maxAllowedRequestSize = 20 * 1000 * 1000) =>
            RequestExecutorBuilderLegacyHelper.SetSchema(
                    services
                        .AddGraphQLServerCore(maxAllowedRequestSize)
                        .AddGraphQL()
                        .AddHttpRequestInterceptor()
                        .AddSubscriptionServices(),
                    schemaFactory)
                .Services;

        [Obsolete(
            "Use the new configuration API -> " +
            "services.AddGraphQLServer().AddQueryType<Query>()...")]
        public static IServiceCollection AddGraphQL(
            this IServiceCollection services,
            ISchemaBuilder schemaBuilder,
            int maxAllowedRequestSize = 20 * 1000 * 1000) =>
            RequestExecutorBuilderLegacyHelper.SetSchemaBuilder(
                services
                    .AddGraphQLServerCore(maxAllowedRequestSize)
                    .AddGraphQL()
                    .AddHttpRequestInterceptor()
                    .AddSubscriptionServices(),
                schemaBuilder)
                .Services;
    }
}

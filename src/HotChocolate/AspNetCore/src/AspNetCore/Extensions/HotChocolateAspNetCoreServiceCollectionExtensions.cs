using System;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.AspNetCore.Extensions
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
                .AddGraphQL(schemaName)
                .AddSubscriptionServices();

        public static IRequestExecutorBuilder AddGraphQLServer(
            this IRequestExecutorBuilder builder,
            NameString schemaName = default) =>
            builder.Services.AddGraphQLServer(schemaName);

        private static IRequestExecutorBuilder AddSubscriptionServices(
            this IRequestExecutorBuilder builder)
        {
            return builder.ConfigureSchemaServices(s =>
            {
                s.TryAddSingleton<IMessagePipeline, DefaultMessagePipeline>();
                s.TryAddSingleton<ISocketSessionInterceptor, DefaultSocketSessionInterceptor>();

                s.AddSingleton<IMessageHandler, DataStartMessageHandler>();
                s.AddSingleton<IMessageHandler, DataStopMessageHandler>();
                s.AddSingleton<IMessageHandler, InitializeConnectionMessageHandler>();
                s.AddSingleton<IMessageHandler, TerminateConnectionMessageHandler>();
            });
        }
    }
}

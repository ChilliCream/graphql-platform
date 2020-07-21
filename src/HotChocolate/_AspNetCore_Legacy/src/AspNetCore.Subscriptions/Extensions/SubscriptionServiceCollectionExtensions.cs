using System.Collections.Generic;
using HotChocolate.AspNetCore.Subscriptions.Interceptors;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Execution;
using HotChocolate.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public static class SubscriptionServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLSubscriptions(
            this IServiceCollection serviceCollection)
        {
            serviceCollection.AddMessageHandlers();
            serviceCollection.AddQueryRequestInterceptor();
            serviceCollection.AddConnectionInterceptor();
            return serviceCollection;
        }

        private static void AddQueryRequestInterceptor(
            this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ISocketQueryRequestInterceptor>(sp =>
                new WebSocketQueryRequestInterceptor(
                    sp.GetService<IQueryRequestInterceptor<HttpContext>>()));
        }

        private static void AddConnectionInterceptor(
            this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IConnectMessageInterceptor>(sp =>
                new ConnectMessageInterceptor(
                    sp.GetService<ISocketConnectionInterceptor<HttpContext>>())
                    );
        }

        private static void AddMessageHandlers(
            this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IMessagePipeline, MessagePipeline>();

            serviceCollection.AddSingleton<IMessageHandler>(sp =>
                new DataStartMessageHandler(
                    sp.GetRequiredService<IQueryExecutor>(),
                    sp.GetServices<ISocketQueryRequestInterceptor>()));

            serviceCollection.AddSingleton<IMessageHandler>(sp =>
                new DataStopMessageHandler());

            serviceCollection.AddSingleton<IMessageHandler>(sp =>
                new InitializeConnectionMessageHandler(
                    sp.GetRequiredService<IConnectMessageInterceptor>()));

            serviceCollection.AddSingleton<IMessageHandler>(sp =>
                new TerminateConnectionMessageHandler());
        }
    }
}

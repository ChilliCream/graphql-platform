using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public static class SubscriptionServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLSubscriptions(
            this IServiceCollection serviceCollection)
        {
            serviceCollection.AddMessageHandlers();
            return serviceCollection;
        }

        internal static void AddMessageHandlers(
            this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IMessageHandler>(sp =>
                new DataStartMessageHandler(
                    sp.GetRequiredService<IQueryExecutor>(),
                    sp.GetService<ICreateRequestInterceptor>()));

            serviceCollection.AddSingleton<IMessageHandler>(sp =>
                new DataStopMessageHandler());

            serviceCollection.AddSingleton<IMessageHandler>(sp =>
                new InitializeConnectionMessageHandler(
                    sp.GetService<IConnectMessageInterceptor>()));

            serviceCollection.AddSingleton<IMessageHandler>(sp =>
                new TerminateConnectionMessageHandler());
        }
    }
}

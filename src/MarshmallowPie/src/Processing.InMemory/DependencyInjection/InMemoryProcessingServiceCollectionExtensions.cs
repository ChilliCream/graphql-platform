using MarshmallowPie;
using MarshmallowPie.Processing;
using MarshmallowPie.Processing.InMemory;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class InMemoryProcessingServiceCollectionExtensions
    {
        public static IServiceCollection AddInMemoryMessageQueue(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<MessageQueue<PublishDocumentMessage>>();
            serviceCollection.AddSingleton<SessionMessageQueue<PublishDocumentEvent>>();
            serviceCollection.AddSingleton<SessionManager>();

            serviceCollection.AddSingleton<IMessageSender<PublishDocumentMessage>>(sp =>
                sp.GetRequiredService<MessageQueue<PublishDocumentMessage>>());
            serviceCollection.AddSingleton<IMessageSender<PublishDocumentEvent>>(sp =>
                sp.GetRequiredService<SessionMessageQueue<PublishDocumentEvent>>());

            serviceCollection.AddSingleton<IMessageReceiver<PublishDocumentMessage>>(sp =>
                sp.GetRequiredService<MessageQueue<PublishDocumentMessage>>());
            serviceCollection.AddSingleton<ISessionMessageReceiver<PublishDocumentEvent>>(sp =>
                sp.GetRequiredService<SessionMessageQueue<PublishDocumentEvent>>());

            serviceCollection.AddSingleton<ISessionCreator>(sp =>
                sp.GetRequiredService<SessionManager>());

            return serviceCollection;
        }
    }
}

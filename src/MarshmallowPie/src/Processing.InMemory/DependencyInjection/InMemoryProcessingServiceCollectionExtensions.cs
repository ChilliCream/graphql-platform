using Microsoft.Extensions.DependencyInjection;
using MarshmallowPie.Processing;
using MarshmallowPie.Processing.InMemory;

namespace MarshmallowPie
{
    public static class InMemoryProcessingServiceCollectionExtensions
    {
        public static IServiceCollection AddInMemoryMessageQueue(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(new MessageQueue<PublishDocumentMessage>());
            serviceCollection.AddSingleton(new SessionMessageQueue<PublishSchemaEvent>());

            serviceCollection.AddSingleton<ISessionCreator, SessionCreator>();

            serviceCollection.AddSingleton<IMessageSender<PublishDocumentMessage>>(sp =>
                sp.GetRequiredService<MessageQueue<PublishDocumentMessage>>());
            serviceCollection.AddSingleton<IMessageSender<PublishSchemaEvent>>(sp =>
                sp.GetRequiredService<SessionMessageQueue<PublishSchemaEvent>>());

            serviceCollection.AddSingleton<IMessageReceiver<PublishDocumentMessage>>(sp =>
                sp.GetRequiredService<MessageQueue<PublishDocumentMessage>>());
            serviceCollection.AddSingleton<ISessionMessageReceiver<PublishSchemaEvent>>(sp =>
                sp.GetRequiredService<SessionMessageQueue<PublishSchemaEvent>>());

            return serviceCollection;
        }
    }
}

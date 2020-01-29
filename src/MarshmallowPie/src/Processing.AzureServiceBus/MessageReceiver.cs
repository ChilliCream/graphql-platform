using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace MarshmallowPie.Processing.AzureServiceBus
{
    public class MessageReceiver<TMessage>
        : IMessageReceiver<TMessage>
        where TMessage : class
    {
        private readonly Func<ISubscriptionClient> _subscriptionClientFactory;

        public MessageReceiver(Func<ISubscriptionClient> subscriptionClientFactory)
        {
            _subscriptionClientFactory = subscriptionClientFactory;
        }

        public ValueTask<IMessageStream<TMessage>> SubscribeAsync(
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<IMessageStream<TMessage>>(
                new MessageStream<TMessage>(_subscriptionClientFactory()));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace MarshmallowPie.Processing.AzureServiceBus
{
    internal class MessageStream<TMessage>
        : IMessageStream<TMessage>
        where TMessage : class
    {
        private readonly ISubscriptionClient _subscriptionClient;
        private MessageEnumerator<TMessage>? _messageEnumerator;

        public MessageStream(ISubscriptionClient subscriptionClient)
        {
            _subscriptionClient = subscriptionClient;
        }

        public IAsyncEnumerator<TMessage?> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            if (_messageEnumerator is null)
            {
                _messageEnumerator = new MessageEnumerator<TMessage>(
                    _subscriptionClient, cancellationToken);
            }

            return _messageEnumerator;
        }

        public ValueTask CompleteAsync()
        {
            if (_messageEnumerator is null)
            {
                throw new InvalidOperationException("There is now message selected.");
            }

            return _messageEnumerator.CompleteAsync();
        }
    }
}

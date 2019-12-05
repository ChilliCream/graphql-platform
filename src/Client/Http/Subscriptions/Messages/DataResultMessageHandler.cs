using System;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Transport;

namespace StrawberryShake.Http.Subscriptions.Messages
{
    public sealed class DataResultMessageHandler
        : MessageHandler<DataResultMessage>
    {
        private readonly ISubscriptionManager _subscriptionManager;

        public DataResultMessageHandler(ISubscriptionManager subscriptionManager)
        {
            _subscriptionManager = subscriptionManager;
        }

        protected override Task HandleAsync(
            ISocketConnection connection,
            DataResultMessage message,
            CancellationToken cancellationToken)
        {
            if (_subscriptionManager.TryGetSubscription(
                message.Id!,
                out ISubscription? subscription))
            {
                return subscription!.OnReceiveResultAsync(message, cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}

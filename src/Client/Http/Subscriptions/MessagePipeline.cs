using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using StrawberryShake.Http.Subscriptions.Messages;
using StrawberryShake.Transport;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    public partial class MessagePipeline
        : IMessagePipeline
    {
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IMessageHandler[] _messageHandlers;

        public MessagePipeline(
            ISubscriptionManager subscriptionManager,
            IEnumerable<IMessageHandler> messageHandlers)
        {
            if (messageHandlers is null)
            {
                throw new ArgumentNullException(nameof(messageHandlers));
            }

            _messageHandlers = messageHandlers.ToArray();
            _subscriptionManager = subscriptionManager
                ?? throw new ArgumentNullException(nameof(subscriptionManager));
        }

        public async Task ProcessAsync(
            ISocketConnection connection,
            ReadOnlySequence<byte> slice,
            CancellationToken cancellationToken)
        {
            if (TryParseMessage(slice, out OperationMessage? message))
            {
                await HandleMessageAsync(connection, message!, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await connection.SendAsync(
                    KeepConnectionAliveMessage.Default.Serialize(),
                    CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }


        private async Task HandleMessageAsync(
            ISocketConnection connection,
            OperationMessage message,
            CancellationToken cancellationToken)
        {
            if (message.Type == "ka")
            {
                return;
            }

            for (var i = 0; i < _messageHandlers.Length; i++)
            {
                IMessageHandler handler = _messageHandlers[i];

                if (handler.CanHandle(message))
                {
                    await handler.HandleAsync(
                            connection,
                            message,
                            cancellationToken)
                        .ConfigureAwait(false);

                    // the message is handled and we are done.
                    return;
                }
            }

            return;

            // TODO : resources
            // throw new NotSupportedException(
            //    "The specified message type is not supported.");
        }
    }
}

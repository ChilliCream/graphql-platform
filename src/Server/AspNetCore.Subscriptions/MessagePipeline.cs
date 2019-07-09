using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using System.Linq;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class MessagePipeline
    {
        private readonly IMessageHandler[] _messageHandlers;

        internal MessagePipeline(IEnumerable<IMessageHandler> messageHandlers)
        {
            if (messageHandlers is null)
            {
                throw new ArgumentNullException(nameof(messageHandlers));
            }

            _messageHandlers = messageHandlers.ToArray();
        }

        internal async Task ProcessAsync(
            ISocketConnection connection,
            ReadOnlySequence<byte> slice,
            CancellationToken cancellationToken)
        {
            if (TryParseMessage(slice, out OperationMessage message))
            {
                await HandleMessageAsync(connection, message, cancellationToken)
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

        private bool TryParseMessage(
            ReadOnlySequence<byte> slice,
            out OperationMessage message)
        {
            throw new NotImplementedException();
        }

        private async Task HandleMessageAsync(
            ISocketConnection connection,
            OperationMessage message,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < _messageHandlers.Length; i++)
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

            // TODO : resources
            throw new NotSupportedException(
                "The specified message type is not supported.");
        }
    }
}

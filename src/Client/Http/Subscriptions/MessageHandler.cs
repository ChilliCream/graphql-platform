using System;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Transport;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    public abstract class MessageHandler<T>
        : IMessageHandler
        where T : OperationMessage
    {
        public bool CanHandle(OperationMessage message)
        {
            return message is T m && CanHandle(m);
        }

        protected virtual bool CanHandle(T message) => true;

        public Task HandleAsync(
            ISocketConnection connection,
            OperationMessage message,
            CancellationToken cancellationToken)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message is T m)
            {
                return HandleAsync(connection, m, cancellationToken);
            }
            else
            {
                throw new NotSupportedException("The specified message type is not supported.");
            }
        }

        protected abstract Task HandleAsync(
            ISocketConnection connection,
            T message,
            CancellationToken cancellationToken);
    }
}

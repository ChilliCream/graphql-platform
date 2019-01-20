#if !ASPNETCLASSIC

using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    /// <summary>
    /// The server may responses with this message to the
    /// GQL_CONNECTION_INIT from client, indicates the
    /// server accepted the connection.
    /// </summary>
    internal sealed class ConnectionInitializeHandler
        : IRequestHandler
    {
        public bool CanHandle(GenericOperationMessage message)
        {
            return message.Type == MessageTypes.Connection.Initialize;
        }

        public async Task HandleAsync(
            IWebSocketContext context,
            GenericOperationMessage message,
            CancellationToken cancellationToken)
        {
            ConnectionStatus connectionStatus =
                await context
                    .OpenAsync(message.Payload.ToDictionary())
                    .ConfigureAwait(false);

            if (connectionStatus.Accepted)
            {
                await context.SendConnectionAcceptMessageAsync(
                    cancellationToken).ConfigureAwait(false);

                await context.SendConnectionKeepAliveMessageAsync(
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await context.SendConnectionErrorMessageAsync(
                    connectionStatus.Response, cancellationToken)
                        .ConfigureAwait(false);

                await context.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}

#endif

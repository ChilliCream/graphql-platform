using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    /// <summary>
    /// The server may responses with this message to the
    /// GQL_CONNECTION_INIT from client, indicates the
    /// server accepted the connection.
    /// </summary>
    public sealed class ConnectionInitializeHandler
        : IRequestHandler
    {
        public bool CanHandle(OperationMessage message)
        {
            return message.Type == MessageTypes.Connection.Initialize;
        }

        public async Task HandleAsync(
            IWebSocketContext context,
            OperationMessage message,
            CancellationToken cancellationToken)
        {
            await context.SendConnectionAcceptMessageAsync(
                cancellationToken);

            await context.SendConnectionKeepAliveMessageAsync(
                cancellationToken);
        }
    }



}

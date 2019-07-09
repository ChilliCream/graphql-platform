using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class InitializeConnectionMessageHandler
        : MessageHandler<InitializeConnectionMessage>
    {
        protected override Task HandleAsync(
            ISocketConnection connection,
            InitializeConnectionMessage message,
            CancellationToken cancellationToken)
        {
            ConnectionStatus connectionStatus =
               await context.OpenAsync(message.Payload.ToDictionary())
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

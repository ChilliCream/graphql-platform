using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    internal sealed class TerminateConnectionMessageHandler
        : MessageHandler<TerminateConnectionMessage>
    {
        protected override Task HandleAsync(
            ISocketConnection connection,
            TerminateConnectionMessage message,
            CancellationToken cancellationToken)
        {
            return connection.CloseAsync(
                "Connection terminated by user.",
                SocketCloseStatus.NormalClosure,
                cancellationToken);
        }
    }
}

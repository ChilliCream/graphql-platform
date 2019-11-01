using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Server;

namespace StrawberryShake.Http.Subscriptions
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

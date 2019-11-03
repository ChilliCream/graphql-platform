using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Server;

namespace StrawberryShake.Http.Subscriptions
{
    public sealed class DataStopMessageHandler
        : MessageHandler<DataStopMessage>
    {
        protected override Task HandleAsync(
            ISocketConnection connection,
            DataStopMessage message,
            CancellationToken cancellationToken)
        {
            connection.Subscriptions.Unregister(message.Id);
            return Task.CompletedTask;
        }
    }
}

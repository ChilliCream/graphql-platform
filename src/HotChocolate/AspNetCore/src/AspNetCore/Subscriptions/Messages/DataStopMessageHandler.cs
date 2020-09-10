using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
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

using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public sealed class ConnectionTerminateHandler
        : IRequestHandler
    {
        public bool CanHandle(OperationMessage message)
        {
            return message.Type == MessageTypes.Connection.Terminate;
        }

        public Task HandleAsync(
            IWebSocketContext context,
            OperationMessage message,
            CancellationToken cancellationToken)
        {
            context.Dispose();
            return Task.CompletedTask;
        }
    }



}

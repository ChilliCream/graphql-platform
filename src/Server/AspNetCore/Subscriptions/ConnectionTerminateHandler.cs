#if !ASPNETCLASSIC

using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class ConnectionTerminateHandler
        : IRequestHandler
    {
        public bool CanHandle(GenericOperationMessage message)
        {
            return message.Type == MessageTypes.Connection.Terminate;
        }

        public async Task HandleAsync(
            IWebSocketContext context,
            GenericOperationMessage message,
            CancellationToken cancellationToken)
        {
            await context.CloseAsync().ConfigureAwait(false);
        }
    }
}

#endif

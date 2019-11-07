using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    public interface IMessageHandler
    {
        Task HandleAsync(
            ISocketConnection connection,
            OperationMessage message,
            CancellationToken cancellationToken);

        bool CanHandle(OperationMessage message);
    }
}

using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Transport;
using StrawberryShake.Transport.Subscriptions;

namespace StrawberryShake.Http.Subscriptions.Messages
{
    public sealed class DataResultMessageHandler
        : MessageHandler<DataResultMessage>
    {
        private readonly ISocketOperationManager _operationManager;

        public DataResultMessageHandler(ISocketOperationManager operationManager)
        {
            _operationManager = operationManager;
        }

        protected override ValueTask HandleAsync(
            ISocketConnection connection,
            DataResultMessage message,
            CancellationToken cancellationToken)
        {
            return _operationManager.ReceiveMessage(message, cancellationToken);
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Transport;
using StrawberryShake.Transport.Subscriptions;

namespace StrawberryShake.Http.Subscriptions.Messages
{
    public sealed class DataResultMessageHandler
        : MessageHandler<DataResultMessage>
    {
        private readonly ISocketProtocol _socketProtocol;

        public DataResultMessageHandler(ISocketProtocol socketProtocol)
        {
            _socketProtocol = socketProtocol;
        }

        protected override ValueTask HandleAsync(
            ISocketClient connection,
            DataResultMessage message,
            CancellationToken cancellationToken)
        {
            if (message.Id is null)
            {
                //TODO: all data messages need a id
                throw new InvalidOperationException();
            }

            return _socketProtocol.Notify(message.Id, message.Payload, cancellationToken);
        }
    }
}

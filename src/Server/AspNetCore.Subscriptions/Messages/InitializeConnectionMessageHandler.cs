using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class InitializeConnectionMessageHandler
        : MessageHandler<InitializeConnectionMessage>
    {
        private readonly IConnectMessageInterceptor _connectMessageInterceptor;

        public InitializeConnectionMessageHandler(
            IConnectMessageInterceptor connectMessageInterceptor)
        {
            _connectMessageInterceptor = connectMessageInterceptor;
        }

        protected override async Task HandleAsync(
            ISocketConnection connection,
            InitializeConnectionMessage message,
            CancellationToken cancellationToken)
        {
            ConnectionStatus connectionStatus;

            if (_connectMessageInterceptor == null)
            {
                connectionStatus = ConnectionStatus.Accept();
            }
            else
            {
                connectionStatus = await _connectMessageInterceptor
                    .OnReceiveAsync(connection, message, cancellationToken);
            }

            if (connectionStatus.Accepted)
            {
                await connection.SendAsync(
                    AcceptConnectionMessage.Default.Serialize(),
                    cancellationToken)
                    .ConfigureAwait(false);

                await connection.SendAsync(
                    KeepConnectionAliveMessage.Default.Serialize(),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var rejectMessage = connectionStatus.Extensions == null
                    ? new RejectConnectionMessage(
                        connectionStatus.Message)
                    : new RejectConnectionMessage(
                        connectionStatus.Message,
                        connectionStatus.Extensions);

                await connection.SendAsync(
                    rejectMessage.Serialize(),
                    cancellationToken)
                    .ConfigureAwait(false);

                // TODO : resources
                await connection.CloseAsync(
                    "Connection was rejected.")
                    .ConfigureAwait(false);
            }
        }
    }
}

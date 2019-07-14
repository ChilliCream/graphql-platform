using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Interceptors;
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
            ConnectionStatus connectionStatus =
                _connectMessageInterceptor == null
                    ? ConnectionStatus.Accept()
                    : await _connectMessageInterceptor.OnReceiveAsync(
                        connection, message, cancellationToken)
                        .ConfigureAwait(false);

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
                    connectionStatus.Message,
                    SocketCloseStatus.PolicyViolation,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}

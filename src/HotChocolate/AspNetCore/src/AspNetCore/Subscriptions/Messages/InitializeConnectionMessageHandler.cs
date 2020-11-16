using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class InitializeConnectionMessageHandler
        : MessageHandler<InitializeConnectionMessage>
    {
        private readonly ISocketSessionInterceptor _socketSessionInterceptor;

        public InitializeConnectionMessageHandler(
            ISocketSessionInterceptor socketSessionInterceptor)
        {
            _socketSessionInterceptor = socketSessionInterceptor;
        }

        protected override async Task HandleAsync(
            ISocketConnection connection,
            InitializeConnectionMessage message,
            CancellationToken cancellationToken)
        {
            ConnectionStatus connectionStatus =
                await _socketSessionInterceptor.OnConnectAsync(
                    connection, message, cancellationToken);

            if (connectionStatus.Accepted)
            {
                await connection.SendAsync(AcceptConnectionMessage.Default, cancellationToken);
                await connection.SendAsync(KeepConnectionAliveMessage.Default, cancellationToken);
            }
            else
            {
                var rejectMessage = connectionStatus.Extensions == null
                    ? new RejectConnectionMessage(connectionStatus.Message)
                    : new RejectConnectionMessage(
                        connectionStatus.Message,
                        connectionStatus.Extensions);

                await connection.SendAsync(
                    rejectMessage.Serialize(),
                    cancellationToken);

                await connection.CloseAsync(
                    connectionStatus.Message,
                    SocketCloseStatus.PolicyViolation,
                    cancellationToken);
            }
        }
    }
}

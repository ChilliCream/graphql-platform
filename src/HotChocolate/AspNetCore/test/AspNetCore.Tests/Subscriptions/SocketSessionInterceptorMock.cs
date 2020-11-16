using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SocketSessionInterceptorMock : DefaultSocketSessionInterceptor
    {
        private readonly ConnectionStatus _connectionStatus;

        public SocketSessionInterceptorMock(ConnectionStatus connectionStatus = null)
        {
            _connectionStatus = connectionStatus ?? ConnectionStatus.Accept();
        }

        public override ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketConnection connection,
            InitializeConnectionMessage message,
            CancellationToken cancellationToken)
        {
            return new ValueTask<ConnectionStatus>(_connectionStatus);
        }

        public override ValueTask OnRequestAsync(
            ISocketConnection connection,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            return default;
        }
    }
}

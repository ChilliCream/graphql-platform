using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Server;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions.Interceptors
{
    public class ConnectMessageInterceptor
        : IConnectMessageInterceptor
    {
        private readonly ISocketConnectionInterceptor<HttpContext> _interceptor;

        public ConnectMessageInterceptor(
            ISocketConnectionInterceptor<HttpContext> connectionInterceptor)
        {
            _interceptor = connectionInterceptor;
        }

        public Task<ConnectionStatus> OnReceiveAsync(
            ISocketConnection connection,
            InitializeConnectionMessage message,
            CancellationToken cancellationToken)
        {
            if (_interceptor != null
                && connection is WebSocketConnection con)
            {
                return _interceptor.OnOpenAsync(
                    con.HttpContext,
                    message.Payload,
                    cancellationToken);
            }

            return Task.FromResult(ConnectionStatus.Accept());
        }
    }
}

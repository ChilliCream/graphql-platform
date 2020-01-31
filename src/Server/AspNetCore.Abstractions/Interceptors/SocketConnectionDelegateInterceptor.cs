using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Interceptors
{
    public delegate Task<ConnectionStatus> OnConnectWebSocketAsync(
        HttpContext context,
        IReadOnlyDictionary<string, object> properties,
        CancellationToken cancellationToken);

    public class SocketConnectionDelegateInterceptor
        : ISocketConnectionInterceptor<HttpContext>
    {
        private readonly OnConnectWebSocketAsync _interceptor;

        public SocketConnectionDelegateInterceptor(
            OnConnectWebSocketAsync interceptor)
        {
            if (interceptor is null)
            {
                throw new ArgumentNullException(nameof(interceptor));
            }
            _interceptor = interceptor;
        }

        public Task<ConnectionStatus> OnOpenAsync(
            HttpContext context,
            IReadOnlyDictionary<string, object> properties,
            CancellationToken cancellationToken)
        {
            return _interceptor(context, properties, cancellationToken);
        }
    }
}

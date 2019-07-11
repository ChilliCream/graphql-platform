using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Server;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
using HttpResponse = Microsoft.Owin.IOwinResponse;
using RequestDelegate = Microsoft.Owin.OwinMiddleware;
#else
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.Interceptors
#else
namespace HotChocolate.AspNetCore.Interceptors
#endif
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

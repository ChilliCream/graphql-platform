using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Server;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions.Interceptors
{
    public class WebSocketQueryRequestInterceptor
        : ISocketQueryRequestInterceptor
    {
        private readonly IQueryRequestInterceptor<HttpContext> _interceptor;

        public WebSocketQueryRequestInterceptor(
            IQueryRequestInterceptor<HttpContext> requestInterceptor)
        {
            _interceptor = requestInterceptor;
        }

        public Task OnCreateAsync(
            ISocketConnection context,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            if (_interceptor != null
                && context is WebSocketConnection connection)
            {
                return _interceptor.OnCreateAsync(
                    connection.HttpContext,
                    requestBuilder,
                    cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}

#if !ASPNETCLASSIC

using System;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    public class SubscriptionMiddleware
    {
        private readonly RequestDelegate _next;

        public SubscriptionMiddleware(
            RequestDelegate next,
            IQueryExecuter queryExecuter,
            QueryMiddlewareOptions options)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            Executer = queryExecuter
                ?? throw new ArgumentNullException(nameof(queryExecuter));
            Options = options
                ?? throw new ArgumentNullException(nameof(options));
        }

        protected IQueryExecuter Executer { get; }

        protected QueryMiddlewareOptions Options { get; }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest
                && context.IsValidPath(Options.SubscriptionPath))
            {
                OnConnectWebSocketAsync onConnect = Options.OnConnectWebSocket
                    ?? GetService<OnConnectWebSocketAsync>(context);
                OnCreateRequestAsync onRequest = Options.OnCreateRequest
                    ?? GetService<OnCreateRequestAsync>(context);

                WebSocketSession session = await WebSocketSession
                    .TryCreateAsync(context, Executer, onConnect, onRequest)
                    .ConfigureAwait(false);

                if (session != null)
                {
                    await session.StartAsync(context.RequestAborted)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        protected T GetService<T>(HttpContext context) =>
            (T)context.RequestServices.GetService(typeof(T));
    }
}

#endif

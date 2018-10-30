using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    public class SubscriptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly QueryExecuter _queryExecuter;

        public SubscriptionMiddleware(
            RequestDelegate next,
            QueryExecuter queryExecuter)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _queryExecuter = queryExecuter
                ?? throw new ArgumentNullException(nameof(queryExecuter));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var session = await WebSocketSession
                    .TryCreateAsync(context, _queryExecuter)
                    .ConfigureAwait(false); ;

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
    }
}

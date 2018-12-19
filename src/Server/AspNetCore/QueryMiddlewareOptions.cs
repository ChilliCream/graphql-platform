using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
#else
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public delegate Task<ConnectionStatus> OnConnectWebSocketAsync(
        HttpContext context,
        IDictionary<string, object> properties,
        CancellationToken cancellationToken);

    public delegate Task OnCreateRequestAsync(
        HttpContext context,
        QueryRequest request,
        IDictionary<string, object> properties,
        CancellationToken cancellationToken);

    public class QueryMiddlewareOptions
    {
        private PathString _path = new PathString("/");
        private PathString _subscriptionPath;

        public int QueryCacheSize { get; set; } = 100;

        public PathString Path
        {
            get => _path;
            set
            {
                if (!value.HasValue)
                {
                    throw new ArgumentException(
                        "The path cannot be empty.");
                }

                _path = value;
                SubscriptionPath = value + new PathString("/ws");
            }
        }

        public PathString SubscriptionPath
        {
            get => _subscriptionPath;
            set
            {
                if (!value.HasValue)
                {
                    throw new ArgumentException(
                        "The subscription-path cannot be empty.");
                }

                _subscriptionPath = value;
            }
        }

        public OnConnectWebSocketAsync OnConnectWebSocket { get; set; }

        public OnCreateRequestAsync OnCreateRequest { get; set; }
    }
}

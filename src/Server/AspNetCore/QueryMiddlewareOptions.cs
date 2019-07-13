using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Server;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
using HotChocolate.AspNetClassic.Interceptors;
#else
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Interceptors;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public class QueryMiddlewareOptions
    {
        private PathString _path = new PathString("/");
        private PathString _subscriptionPath = new PathString("/");

        [Obsolete(
            "Use query execution options.",
            true)]
        public int QueryCacheSize { get; set; } = 100;

        public int MaxRequestSize { get; set; } = 20 * 1000 * 1000;

        public ParserOptions ParserOptions { get; set; } = new ParserOptions();

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
                SubscriptionPath = value;
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

        [Obsolete(
            "Use serviceCollection.AddSocketConnectionInterceptor()",
            true)]
        public OnConnectWebSocketAsync OnConnectWebSocket { get; set; }

        [Obsolete(
            "Use serviceCollection.AddQueryRequestInterceptor()",
            true)]
        public OnCreateRequestAsync OnCreateRequest { get; set; }
    }
}

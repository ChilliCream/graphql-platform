using System;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Interceptors;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore
{
    public class QueryMiddlewareOptions
    {
        private PathString _path = new PathString("/");
        private PathString _subscriptionPath = new PathString("/");
        private ParserOptions _parserOptions = new ParserOptions();

        [Obsolete("Use query execution options.", true)]
        public int QueryCacheSize { get; set; } = 100;

        public int MaxRequestSize { get; set; } = 20 * 1000 * 1000;

        public ParserOptions ParserOptions
        {
            get => _parserOptions;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _parserOptions = value;
            }
        }

        public PathString Path
        {
            get => _path;
            set
            {
                if (!value.HasValue)
                {
                    // TODO : resources
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
                    // TODO : resources
                    throw new ArgumentException(
                        "The subscription-path cannot be empty.");
                }

                _subscriptionPath = value;
            }
        }

        public bool EnableSubscriptions { get; set; } = true;

        [Obsolete(
            "Use serviceCollection.AddSocketConnectionInterceptor()",
            true)]
        public OnConnectWebSocketAsync? OnConnectWebSocket { get; set; }

        [Obsolete(
            "Use serviceCollection.AddQueryRequestInterceptor()",
            true)]
        public OnCreateRequestAsync? OnCreateRequest { get; set; }
    }
}

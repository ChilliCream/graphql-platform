using System;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SubscriptionMiddlewareOptions
    {
        private PathString _subscriptionPath = new PathString("/");

        public ParserOptions ParserOptions { get; set; } = new ParserOptions();

        public PathString Path
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
    }
}

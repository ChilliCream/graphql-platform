using System;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    public class GraphiQLOptionsBase
        : IUIOptionsAccessor
    {
        private bool _pathIsSet;
        private bool _subscriptionPathIsSet;

        private readonly PathString _defaultPath;
        private PathString _path;
        private PathString _queryPath = new PathString("/");
        private PathString _subscriptionPath = new PathString("/");

        protected GraphiQLOptionsBase(PathString defaultPath)
        {
            _path = _defaultPath = defaultPath;
        }


        /// <summary>
        /// The path of the GraphiQL middleware.
        /// </summary>
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
                _pathIsSet = true;
            }
        }

        /// <summary>
        /// The path of the query middleware.
        /// This is basically where GraphiQL
        /// send its requests to execute its queries.
        /// </summary>
        public PathString QueryPath
        {
            get => _queryPath;
            set
            {
                if (!value.HasValue)
                {
                    throw new ArgumentException(
                        "The query-path cannot be empty.");
                }

                _queryPath = value;

                if (!_subscriptionPathIsSet)
                {
                    _subscriptionPath = value;
                }

                if (!_pathIsSet)
                {
                    _path = value.Add(_defaultPath);
                }
            }
        }

        /// <summary>
        /// The path of the subscription middleware.
        /// By default this will be the same as the query path.
        /// </summary>
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
                _subscriptionPathIsSet = true;
            }
        }

        /// <summary>
        /// Defines if the GraphiQL client shall handle subscriptions.
        /// </summary>
        public bool EnableSubscription { get; set; } = true;
    }
}

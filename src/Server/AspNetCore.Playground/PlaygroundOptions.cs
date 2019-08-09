using System;

#if ASPNETCLASSIC
using Microsoft.Owin;
#else
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.Playground
#else
namespace HotChocolate.AspNetCore.Playground
#endif
{
    public class PlaygroundOptions
    {
        private bool _pathIsSet = false;
        private bool _subscriptionPathIsSet = false;

        private PathString _path = new PathString("/playground");
        private PathString _queryPath = new PathString("/");
        private PathString _subscriptionPath = new PathString("/");

        /// <summary>
        /// The path of the playground middleware.
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
        /// This is basically where playground
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
                    _path = value.Add(new PathString("/playground"));
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

        public bool EnableSubscription { get; set; } = true;
    }
}

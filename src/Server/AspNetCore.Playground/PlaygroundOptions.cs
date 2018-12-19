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
        private PathString _path = new PathString("/playground");
        private PathString _queryPath = new PathString("/");
        private PathString _subscriptionPath = new PathString("/ws");

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
            }
        }

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
                _subscriptionPath = value.Add(new PathString("/ws"));
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
    }
}

using System;

namespace HotChocolate.Resolvers
{
    internal sealed class DirectiveDelegateMiddleware
        : IDirectiveMiddleware
    {
        public DirectiveDelegateMiddleware(
            string directiveName,
            Middleware middleware)
        {
            if (string.IsNullOrEmpty(directiveName))
            {
                throw new ArgumentNullException(nameof(directiveName));
            }

            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            DirectiveName = directiveName;
            Middleware = middleware;
        }

        public string DirectiveName { get; }

        public Middleware Middleware { get; }
    }
}

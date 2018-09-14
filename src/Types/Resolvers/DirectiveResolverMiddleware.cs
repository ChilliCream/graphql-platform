using System;

namespace HotChocolate.Resolvers
{
    internal class DirectiveResolverMiddleware
        : IDirectiveMiddleware
    {
        public DirectiveResolverMiddleware(
            string directiveName,
            DirectiveResolver resolver)
        {
            if (string.IsNullOrEmpty(directiveName))
            {
                throw new ArgumentNullException(nameof(directiveName));
            }

            DirectiveName = directiveName;
            Resolver = resolver
                ?? throw new ArgumentNullException(nameof(resolver));
        }

        public string DirectiveName { get; }
        public MiddlewareKind Kind => MiddlewareKind.OnInvoke;
        public DirectiveResolver Resolver { get; }
    }
}

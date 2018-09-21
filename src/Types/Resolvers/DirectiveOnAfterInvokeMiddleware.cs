using System;

namespace HotChocolate.Resolvers
{
    internal sealed class DirectiveOnAfterInvokeMiddleware
        : IDirectiveMiddleware
    {
        public DirectiveOnAfterInvokeMiddleware(
            string directiveName,
            OnAfterInvokeResolverAsync onAfterInvokeResolver)
        {
            if (string.IsNullOrEmpty(directiveName))
            {
                throw new ArgumentNullException(nameof(directiveName));
            }

            DirectiveName = directiveName;
            OnAfterInvokeResolver = onAfterInvokeResolver
                ?? throw new ArgumentNullException(
                        nameof(onAfterInvokeResolver));
        }

        public string DirectiveName { get; }
        public MiddlewareKind Kind => MiddlewareKind.OnAfterInvoke;
        public OnAfterInvokeResolverAsync OnAfterInvokeResolver { get; }
    }
}

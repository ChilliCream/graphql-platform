using System;

namespace HotChocolate.Resolvers
{
    internal sealed class DirectiveOnBeforeInvokeMiddleware
        : IDirectiveMiddleware
    {
        public DirectiveOnBeforeInvokeMiddleware(
            string directiveName,
            OnBeforeInvokeResolverAsync onBeforeInvokeResolver)
        {
            if (string.IsNullOrEmpty(directiveName))
            {
                throw new ArgumentNullException(nameof(directiveName));
            }

            DirectiveName = directiveName;
            OnBeforeInvokeResolver = onBeforeInvokeResolver
                ?? throw new ArgumentNullException(
                        nameof(onBeforeInvokeResolver));
        }

        public string DirectiveName { get; }
        public MiddlewareKind Kind => MiddlewareKind.OnBeforeInvoke;
        public OnBeforeInvokeResolverAsync OnBeforeInvokeResolver { get; }
    }
}

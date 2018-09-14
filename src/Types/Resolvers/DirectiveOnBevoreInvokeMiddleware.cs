using System;

namespace HotChocolate.Resolvers
{
    internal class DirectiveOnBeforeInvokeMiddleware
        : IDirectiveMiddleware
    {
        public DirectiveOnBeforeInvokeMiddleware(
            string directiveName,
            OnBeforeInvokeResolver onBeforeInvokeResolver)
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
        public OnBeforeInvokeResolver OnBeforeInvokeResolver { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class DirectiveMiddlewareDescriptor
        : IDirectiveMiddlewareDescriptor
    {
        public DirectiveMiddlewareDescriptor(
            DirectiveMethodMiddleware directiveMethodMiddleware)
        {
            if (directiveMethodMiddleware == null)
            {
                throw new ArgumentNullException(
                    nameof(directiveMethodMiddleware));
            }

            DirectiveName = directiveMethodMiddleware.DirectiveName;
            Kind = directiveMethodMiddleware.Kind;
            Type = directiveMethodMiddleware.Type;
            Method = directiveMethodMiddleware.Method;

            Arguments = FieldResolverDiscoverer
                .DiscoverArguments(Method);
            IsAsync = typeof(Task).IsAssignableFrom(Method.ReturnType);
        }

        public string DirectiveName { get; }

        public MiddlewareKind Kind { get; }

        public Type Type { get; }

        public MethodInfo Method { get; }

        public IReadOnlyCollection<ArgumentDescriptor> Arguments { get; }

        public bool IsAsync { get; }
    }
}

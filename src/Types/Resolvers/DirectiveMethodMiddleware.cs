using System;
using System.Reflection;

namespace HotChocolate.Resolvers
{
    internal class DirectiveMethodMiddleware
        : IDirectiveMiddleware
    {
        public DirectiveMethodMiddleware(
            string directiveName,
            MiddlewareKind kind,
            Type type,
            MethodInfo method)
        {
            if (string.IsNullOrEmpty(directiveName))
            {
                throw new ArgumentNullException(nameof(directiveName));
            }

            DirectiveName = directiveName;
            Kind = kind;
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Method = method ?? throw new ArgumentNullException(nameof(method));
        }

        public string DirectiveName { get; }
        public MiddlewareKind Kind { get; }
        public Type Type { get; }
        public MethodInfo Method { get; }
    }
}

using System;
using System.Collections.Generic;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal class ResolverBuilderResult
    {
        public ResolverBuilderResult(
            IReadOnlyCollection<FieldResolver> resolvers,
            IReadOnlyCollection<IDirectiveMiddleware> middlewares)
        {
            Resolvers = resolvers
                ?? throw new ArgumentNullException(nameof(resolvers));
            Middlewares = middlewares
                ?? throw new ArgumentNullException(nameof(middlewares));
        }

        private ResolverBuilderResult()
        {
            Resolvers = Array.Empty<FieldResolver>();
            Middlewares = Array.Empty<IDirectiveMiddleware>();
        }

        public IReadOnlyCollection<FieldResolver> Resolvers { get; }
        public IReadOnlyCollection<IDirectiveMiddleware> Middlewares { get; }

        public static ResolverBuilderResult Empty { get; } =
            new ResolverBuilderResult();
    }
}

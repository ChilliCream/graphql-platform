using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus
{
    internal class ResolverCollection
        : IResolverCollection
    {
        private readonly IImmutableDictionary<FieldReference, IResolver> _resolvers;

        public ResolverCollection(IDictionary<FieldReference, ResolveAsync> resolvers)
        {
            if (resolvers == null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }
            _resolvers = resolvers.ToImmutableDictionary(t => t.Key, t => (IResolver)new Resolver(t.Value));
        }

        public bool TryGetResolver(string typeName, string fieldName, out IResolver resolver)
        {

            return _resolvers.TryGetValue(FieldReference.Create(typeName, fieldName), out resolver);
        }

        private class Resolver
            : IResolver
        {
            private ResolveAsync _resolveAsync;

            public Resolver(ResolveAsync resolveAsync)
            {
                _resolveAsync = resolveAsync;
            }

            public Task<object> ResolveAsync(IResolverContext context, CancellationToken cancellationToken)
            {
                return _resolveAsync(context, cancellationToken);
            }
        }
    }

}

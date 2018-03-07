using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Resolvers
{
    public class ResolverCollection
        : IResolverCollection
    {
        private readonly object _sync = new object();
        private readonly IServiceProvider _serviceProvider;
        private readonly ImmutableDictionary<FieldReference, ResolverFactory> _resolverFactories;
        private ImmutableDictionary<FieldReference, ResolverDelegate> _resolvers = 
            ImmutableDictionary<FieldReference, ResolverDelegate>.Empty;

        internal ResolverCollection(IServiceProvider serviceProvider, 
            IDictionary<FieldReference, ResolverFactory> resolverFactories)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (resolverFactories == null)
            {
                throw new ArgumentNullException(nameof(resolverFactories));
            }

            _serviceProvider = serviceProvider;
            _resolverFactories = resolverFactories.ToImmutableDictionary(t => t.Key, t => t.Value);
        }

        public bool TryGetResolver(string typeName, string fieldName, out ResolverDelegate resolver)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            FieldReference fieldReference = FieldReference.Create(typeName, fieldName);
            if (!_resolvers.TryGetValue(fieldReference, out resolver))
            {
                ResolverFactory resolverFactory;
                if (_resolverFactories.TryGetValue(fieldReference, out resolverFactory))
                {
                    lock (_sync)
                    {
                        resolver = resolverFactory(_serviceProvider);
                        _resolvers = _resolvers.SetItem(fieldReference, resolver);
                        return true;
                    }
                }
                return false;
            }
            return true;
        }
    }
}
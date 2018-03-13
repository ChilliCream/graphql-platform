using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Prometheus.Abstractions;

namespace Prometheus.Resolvers
{
    public class ResolverCollection
        : IResolverCollection
    {
        private IReadOnlyDictionary<FieldReference, ResolverDelegate> _resolvers;

        internal ResolverCollection(
            IReadOnlyDictionary<FieldReference, ResolverDelegate> resolvers)
        {
            if (resolvers == null)
            {
                throw new ArgumentNullException(nameof(_resolvers));
            }

            _resolvers = resolvers;
        }

        public bool TryGetResolver(
            string typeName, string fieldName,
            out ResolverDelegate resolver)
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
            return _resolvers.TryGetValue(fieldReference, out resolver);
        }
    }
}
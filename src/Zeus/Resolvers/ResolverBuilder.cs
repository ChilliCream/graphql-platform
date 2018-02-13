using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Resolvers
{
    public class ResolverBuilder
        : IResolverBuilder
    {
        private readonly Dictionary<FieldReference, Func<IServiceProvider, IResolver>> _resolvers = new Dictionary<FieldReference, Func<IServiceProvider, IResolver>>();

        private ResolverBuilder() { }

        public IResolverBuilder Add(string typeName, string fieldName, Func<IServiceProvider, IResolver> resolverFactory)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (resolverFactory == null)
            {
                throw new ArgumentNullException(nameof(resolverFactory));
            }

            _resolvers[FieldReference.Create(typeName, fieldName)] = resolverFactory;
            return this;
        }

        public IResolverCollection Build()
        {
            return new ResolverCollection(_resolvers);
        }

        public static IResolverBuilder Create()
        {
            return new ResolverBuilder();
        }
    }
}

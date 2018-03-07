using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Resolvers
{
    public class ResolverBuilder
        : IResolverBuilder
    {
        private readonly Dictionary<FieldReference, ResolverFactory> _resolvers =
            new Dictionary<FieldReference, ResolverFactory>();
        private readonly IServiceProvider _serviceProvider;

        private ResolverBuilder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IResolverBuilder Add(string typeName, string fieldName, ResolverFactory resolverFactory)
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
            return new ResolverCollection(_serviceProvider, _resolvers);
        }

        public static IResolverBuilder Create()
        {
            return new ResolverBuilder(DefaultServiceProvider.Instance);
        }

        public static IResolverBuilder Create(IServiceProvider serviceProvider)
        {
            return new ResolverBuilder(serviceProvider);
        }
    }
}

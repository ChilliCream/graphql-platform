using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    public class ResolverBuilder
        : IResolverBuilder
    {
        private readonly List<ResolverFactoryDelegate> _resolverFactories =
            new List<ResolverFactoryDelegate>();

        public IResolverBuilder Add(
            ResolverFactoryDelegate resolverFactory)
        {

            if (resolverFactory == null)
            {
                throw new ArgumentNullException(nameof(resolverFactory));
            }

            _resolverFactories.Add(resolverFactory);
            return this;
        }

        public IResolverCollection Build(ISchemaDocument schema,
            IServiceProvider serviceProvider)
        {
            Dictionary<FieldReference, ResolverDelegate> resolvers =
                new Dictionary<FieldReference, ResolverDelegate>();

            foreach (var resolverFactory in _resolverFactories)
            {
                resolverFactory(schema, serviceProvider,
                    (typeName, fieldName, resolver) =>
                    {
                        FieldReference fieldReference = FieldReference
                            .Create(typeName, fieldName);
                        resolvers[fieldReference] = resolver;
                    });
            }

            return new ResolverCollection(resolvers);
        }

        public static IResolverBuilder Create()
        {
            return new ResolverBuilder();
        }


    }
}

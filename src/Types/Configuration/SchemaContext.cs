using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class SchemaContext
        : ISchemaContext
    {
        private readonly TypeRegistry _typeRegistry;
        private readonly ResolverRegistry _resolverRegistry;

        public SchemaContext(ServiceManager serviceManager)
        {
            ServiceManager = serviceManager;
            _typeRegistry = new TypeRegistry(serviceManager);
            _resolverRegistry = new ResolverRegistry();
        }

        public ITypeRegistry Types => _typeRegistry;

        public IResolverRegistry Resolvers => _resolverRegistry;

        public ServiceManager ServiceManager { get; }

        public IEnumerable<SchemaError> CompleteTypes()
        {
            // compile resolvers
            _resolverRegistry.BuildResolvers();

            // complete types
            List<SchemaError> errors = new List<SchemaError>();
            foreach (INamedType namedType in _typeRegistry.GetTypes())
            {
                if (namedType is INeedsInitialization init)
                {
                    var initializationContext = new TypeInitializationContext(
                        this, e => errors.Add(e), namedType, false);
                    init.CompleteType(initializationContext);
                }
            }
            return errors;
        }
    }
}

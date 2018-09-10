using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Runtime;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class SchemaContext
        : ISchemaContext
    {
        private readonly ServiceFactory _serviceFactory;
        private readonly TypeRegistry _typeRegistry;
        private readonly DirectiveRegistry _directiveRegistry;
        private readonly ResolverRegistry _resolverRegistry;

        public SchemaContext()
        {
            _serviceFactory = new ServiceFactory();
            _typeRegistry = new TypeRegistry(_serviceFactory);
            _resolverRegistry = new ResolverRegistry();
            _directiveRegistry = new DirectiveRegistry();
            DataLoaders = new List<DataLoaderDescriptor>();
            CustomContexts = new List<CustomContextDescriptor>();
        }

        public ITypeRegistry Types => _typeRegistry;

        public IResolverRegistry Resolvers => _resolverRegistry;

        public IDirectiveRegistry Directives => _directiveRegistry;

        public IServiceProvider Services => _serviceFactory.Services;

        public ICollection<DataLoaderDescriptor> DataLoaders { get; }

        public ICollection<CustomContextDescriptor> CustomContexts { get; }

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

        public IEnumerable<SchemaError> CompleteDirectives()
        {
            List<SchemaError> errors = new List<SchemaError>();
            foreach (INeedsInitialization directive in _directiveRegistry.GetDirectiveTypes()
                .Cast<INeedsInitialization>())
            {
                var initializationContext = new TypeInitializationContext(
                    this, e => errors.Add(e));
                directive.CompleteType(initializationContext);
            }
            return errors;
        }

        public void RegisterServiceProvider(IServiceProvider services)
        {
            _serviceFactory.Services = services;
        }
    }
}

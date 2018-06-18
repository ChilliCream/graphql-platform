using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class TypeFinalizer
    {
        private readonly SchemaConfiguration _schemaConfiguration;
        private List<SchemaError> _errors = new List<SchemaError>();

        public TypeFinalizer(SchemaConfiguration schemaConfiguration)
        {
            _schemaConfiguration = schemaConfiguration
                ?? throw new ArgumentNullException(nameof(schemaConfiguration));
        }

        public IReadOnlyCollection<SchemaError> Errors => _errors;

        public void FinalizeTypes(SchemaContext context)
        {
            // register types and their dependencies
            RegisterTypes(context);

            // finalize and register .net type to schema type bindings
            RegisterTypeBindings(context.Types);

            // finalize and register field resolver bindings
            RegisterFieldResolvers(context);

            // compile resolvers and finalize types
            _errors.AddRange(context.CompleteTypes());
        }

        private void RegisterTypes(ISchemaContext context)
        {
            TypeRegistrar typeRegistrar = new TypeRegistrar(
                context.Types.GetTypes().Concat(
                    _schemaConfiguration.Types));
            typeRegistrar.RegisterTypes(context);
            _errors.AddRange(typeRegistrar.Errors);
        }

        private void RegisterTypeBindings(ITypeRegistry typeRegistry)
        {
            TypeBindingRegistrar typeBindingRegistrar = new TypeBindingRegistrar(
                _schemaConfiguration.TypeBindings);
            typeBindingRegistrar.RegisterTypeBindings(typeRegistry);
        }

        private void RegisterFieldResolvers(ISchemaContext context)
        {
            ResolverRegistrar resolverRegistrar = new ResolverRegistrar(
                _schemaConfiguration.ResolverBindings);
            resolverRegistrar.RegisterResolvers(context);
        }
    }
}

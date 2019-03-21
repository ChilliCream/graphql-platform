using System;
using System.Collections.Generic;

namespace HotChocolate.Configuration
{
    internal class TypeFinalizer
    {
        private readonly SchemaConfiguration _schemaConfiguration;
        private readonly List<SchemaError> _errors = new List<SchemaError>();

        public TypeFinalizer(SchemaConfiguration schemaConfiguration)
        {
            _schemaConfiguration = schemaConfiguration
                ?? throw new ArgumentNullException(nameof(schemaConfiguration));
        }

        public IReadOnlyCollection<SchemaError> Errors => _errors;

        public void FinalizeTypes(SchemaContext context, string queryTypeName)
        {
            // register types and their dependencies
            RegisterTypes(context, queryTypeName);

            // finalize and register .net type to schema type bindings
            RegisterTypeBindings(context);

            // finalize and register field resolver bindings
            RegisterFieldResolvers(context);

            // compile resolvers and finalize types
            _errors.AddRange(context.CompleteTypes());
        }

        private void RegisterTypes(ISchemaContext context, string queryTypeName)
        {
            var typeRegistrar = new TypeRegistrar(
                context.Types.GetTypes());
            typeRegistrar.RegisterTypes(context, queryTypeName);
            _errors.AddRange(typeRegistrar.Errors);
        }

        private void RegisterTypeBindings(ISchemaContext context)
        {
            var typeBindingRegistrar = new TypeBindingRegistrar(
                _schemaConfiguration.TypeBindings);
            typeBindingRegistrar.RegisterTypeBindings(context.Types);
            ((TypeRegistry)context.Types).CompleteRegistartion();
        }

        private void RegisterFieldResolvers(ISchemaContext context)
        {
            var resolverRegistrar = new ResolverRegistrar(
                _schemaConfiguration.ResolverBindings);
            resolverRegistrar.RegisterResolvers(context);
        }
    }
}

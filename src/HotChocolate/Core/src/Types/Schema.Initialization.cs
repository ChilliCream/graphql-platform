using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate
{
    public partial class Schema
    {
        private readonly Action<ISchemaTypeDescriptor> _configure;
        private bool _sealed;

        protected internal Schema()
        {
            _configure = Configure;
        }

        public Schema(Action<ISchemaTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        protected virtual void Configure(ISchemaTypeDescriptor descriptor)
        {
        }

        protected sealed override SchemaTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = SchemaTypeDescriptor.New(
                context.DescriptorContext,
                GetType());

            _configure(descriptor);

            return descriptor.CreateDefinition();
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            SchemaTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            context.RegisterDependencyRange(
                definition.GetDirectives().Select(t => t.TypeReference),
                TypeDependencyKind.Completed);

            context.RegisterDependencyRange(
                definition.GetDirectives().Select(t => t.Reference));
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            SchemaTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            var directives = new DirectiveCollection(this, definition.GetDirectives());
            directives.CompleteCollection(context);
            Directives = directives;
            Services = context.Services;
        }

        internal void CompleteSchema(
            SchemaTypesDefinition schemaTypesDefinition)
        {
            if (schemaTypesDefinition is null)
            {
                throw new ArgumentNullException(nameof(schemaTypesDefinition));
            }

            if (_sealed)
            {
                throw new InvalidOperationException(
                    "This schema is already sealed and cannot be mutated.");
            }

            DirectiveTypes = schemaTypesDefinition.DirectiveTypes;
            _types = new SchemaTypes(schemaTypesDefinition);
            _directiveTypes = DirectiveTypes.ToDictionary(t => t.Name);
            _sealed = true;
        }
    }
}

using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Stitching.Types
{
    internal class SchemaDefinitionSchemaInterceptor : SchemaInterceptor
    {
        private readonly PublishSchemaDefinitionDescriptor _descriptor;

        public SchemaDefinitionSchemaInterceptor(
            PublishSchemaDefinitionDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        public override void OnBeforeCreate(
            IDescriptorContext context,
            ISchemaBuilder schemaBuilder) =>
            context.GetOrAddSchemaDefinitions();

        public override void OnAfterCreate(
            IDescriptorContext context,
            ISchema schema)
        {
            context
                .GetOrAddSchemaDefinitions()
                .Add(_descriptor.Build(context, schema));
        }
    }
}

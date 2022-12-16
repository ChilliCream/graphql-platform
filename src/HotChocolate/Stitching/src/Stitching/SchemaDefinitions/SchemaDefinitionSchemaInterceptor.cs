using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Stitching.SchemaDefinitions;

internal sealed class SchemaDefinitionSchemaInterceptor : TypeInterceptor
{
    private readonly PublishSchemaDefinitionDescriptor _descriptor;

    public SchemaDefinitionSchemaInterceptor(
        PublishSchemaDefinitionDescriptor descriptor)
    {
        _descriptor = descriptor;
    }

    public override void OnBeforeCreateSchema(
        IDescriptorContext context,
        ISchemaBuilder schemaBuilder)
        => context.GetOrAddSchemaDefinitions();

    public override void OnAfterCreateSchema(
        IDescriptorContext context,
        ISchema schema)
    {
        context
            .GetOrAddSchemaDefinitions()
            .Add(_descriptor.Build(context, schema));
    }
}

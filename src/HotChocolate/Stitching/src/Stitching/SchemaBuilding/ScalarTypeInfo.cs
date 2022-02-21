using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal class ScalarTypeInfo
    : TypeInfo<ScalarTypeDefinitionNode>
{
    public ScalarTypeInfo(
        ScalarTypeDefinitionNode typeDefinition,
        ISchemaInfo schema)
        : base(typeDefinition, schema)
    {
    }
}

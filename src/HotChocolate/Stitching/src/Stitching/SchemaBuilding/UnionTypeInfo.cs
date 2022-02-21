using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal class UnionTypeInfo
    : TypeInfo<UnionTypeDefinitionNode>
{
    public UnionTypeInfo(
        UnionTypeDefinitionNode typeDefinition,
        ISchemaInfo schema)
        : base(typeDefinition, schema)
    {
    }
}

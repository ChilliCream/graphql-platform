using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal class EnumTypeInfo
    : TypeInfo<EnumTypeDefinitionNode>
{
    public EnumTypeInfo(
        EnumTypeDefinitionNode typeDefinition,
        ISchemaInfo schema)
        : base(typeDefinition, schema)
    {
    }
}

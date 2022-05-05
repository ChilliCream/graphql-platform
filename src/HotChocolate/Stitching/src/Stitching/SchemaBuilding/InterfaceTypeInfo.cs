using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal class InterfaceTypeInfo
    : TypeInfo<InterfaceTypeDefinitionNode>
{
    public InterfaceTypeInfo(
        InterfaceTypeDefinitionNode typeDefinition,
        ISchemaInfo schema)
        : base(typeDefinition, schema)
    {
    }
}

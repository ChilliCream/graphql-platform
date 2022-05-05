using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal class InputObjectTypeInfo
    : TypeInfo<InputObjectTypeDefinitionNode>
{
    public InputObjectTypeInfo(
        InputObjectTypeDefinitionNode typeDefinition,
        ISchemaInfo schema)
        : base(typeDefinition, schema)
    {
    }
}

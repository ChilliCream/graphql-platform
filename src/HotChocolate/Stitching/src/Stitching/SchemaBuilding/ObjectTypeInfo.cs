using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal class ObjectTypeInfo
    : TypeInfo<ObjectTypeDefinitionNode>
{
    public ObjectTypeInfo(
        ObjectTypeDefinitionNode typeDefinition,
        ISchemaInfo schema)
        : base(typeDefinition, schema)
    {
    }
}

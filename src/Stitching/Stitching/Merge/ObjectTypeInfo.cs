using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
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
}

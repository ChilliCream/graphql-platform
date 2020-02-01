using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
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
}

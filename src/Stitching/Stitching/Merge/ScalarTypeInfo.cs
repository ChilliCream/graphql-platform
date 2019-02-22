using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
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
}

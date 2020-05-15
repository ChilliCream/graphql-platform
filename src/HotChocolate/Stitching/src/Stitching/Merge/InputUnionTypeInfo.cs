using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    internal class InputUnionTypeInfo
        : TypeInfo<InputUnionTypeDefinitionNode>
    {
        public InputUnionTypeInfo(
            InputUnionTypeDefinitionNode typeDefinition,
            ISchemaInfo schema)
            : base(typeDefinition, schema)
        {
        }
    }
}

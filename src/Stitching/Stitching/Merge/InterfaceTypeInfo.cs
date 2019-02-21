using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
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
}

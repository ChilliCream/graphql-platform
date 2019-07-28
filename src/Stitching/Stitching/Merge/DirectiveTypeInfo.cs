using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    public class DirectiveTypeInfo : IDirectiveTypeInfo
    {
        public DirectiveTypeInfo(
            DirectiveDefinitionNode definition,
            ISchemaInfo schema)
        {
            Definition = definition;
            Schema = schema;
        }

        public DirectiveDefinitionNode Definition { get; }

        public ISchemaInfo Schema { get; }
    }
}

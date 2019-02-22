using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    public class DirectiveInfo
        : IDirectiveInfo
    {
        private DirectiveInfo(
            DirectiveDefinitionNode definition,
            ISchemaInfo schema)
        {
            Definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));

        }
        public DirectiveDefinitionNode Definition { get; }

        public ISchemaInfo Schema { get; }

        public static DirectiveInfo Create(
            DirectiveDefinitionNode directiveDefinition,
            ISchemaInfo schema)
        {
            return new DirectiveInfo(directiveDefinition, schema);
        }
    }
}

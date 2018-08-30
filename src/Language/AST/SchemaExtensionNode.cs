using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class SchemaExtensionNode
        : SchemaDefinitionNodeBase
        , ITypeExtensionNode
    {
        public SchemaExtensionNode(
            Location location,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<OperationTypeDefinitionNode> operationTypes)
            : base(location, directives, operationTypes)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.SchemaExtension;
    }
}

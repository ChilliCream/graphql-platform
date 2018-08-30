using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class SchemaDefinitionNode
        : SchemaDefinitionNodeBase
        , ITypeSystemDefinitionNode
    {
        public SchemaDefinitionNode(
            Location location,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<OperationTypeDefinitionNode> operationTypes)
            : base(location, directives, operationTypes)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.SchemaDefinition;
    }
}

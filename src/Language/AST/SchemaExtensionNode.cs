using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class SchemaExtensionNode
        : ITypeExtensionNode
        , IHasDirectives
    {
        public SchemaExtensionNode(
            Location location,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<OperationTypeDefinitionNode> operationTypes)
        {
            Location = location;
            Directives = directives;
            OperationTypes = operationTypes;
        }

        public NodeKind Kind { get; } = NodeKind.SchemaExtension;
        public Location Location { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<OperationTypeDefinitionNode> OperationTypes { get; }
    }
}

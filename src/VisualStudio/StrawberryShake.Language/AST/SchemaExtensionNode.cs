using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class SchemaExtensionNode
        : SchemaDefinitionNodeBase
        , ITypeSystemExtensionNode
    {
        public SchemaExtensionNode(
            Location? location,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
            : base(location, directives, operationTypes)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.SchemaExtension;

        public SchemaExtensionNode WithLocation(Location? location)
        {
            return new SchemaExtensionNode(
                Location, Directives, OperationTypes);
        }

        public SchemaExtensionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new SchemaExtensionNode(
                Location, directives, OperationTypes);
        }

        public SchemaExtensionNode WithOperationTypes(
            IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
        {
            return new SchemaExtensionNode(
                Location, Directives, operationTypes);
        }
    }
}

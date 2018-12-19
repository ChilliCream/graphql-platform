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

        public SchemaExtensionNode WithLocation(Location location)
        {
            return new SchemaExtensionNode(
                Location, Directives, OperationTypes);
        }

        public SchemaExtensionNode WithDirectives(
            IReadOnlyCollection<DirectiveNode> directives)
        {
            return new SchemaExtensionNode(
                Location, directives, OperationTypes);
        }

        public SchemaExtensionNode WithOperationTypes(
            IReadOnlyCollection<OperationTypeDefinitionNode> operationTypes)
        {
            return new SchemaExtensionNode(
                Location, Directives, operationTypes);
        }
    }
}

using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class SchemaDefinitionNode
        : SchemaDefinitionNodeBase
        , ITypeSystemDefinitionNode
    {
        public SchemaDefinitionNode(
            Location location,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
            : base(location, directives, operationTypes)
        {
        }

        public override NodeKind Kind { get; } = NodeKind.SchemaDefinition;

        public SchemaDefinitionNode WithLocation(Location location)
        {
            return new SchemaDefinitionNode(
                Location, Directives, OperationTypes);
        }

        public SchemaDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new SchemaDefinitionNode(
                Location, directives, OperationTypes);
        }

        public SchemaDefinitionNode WithOperationTypes(
            IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
        {
            return new SchemaDefinitionNode(
                Location, Directives, operationTypes);
        }
    }
}

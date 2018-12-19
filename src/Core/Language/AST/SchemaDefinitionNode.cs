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

        public SchemaDefinitionNode WithLocation(Location location)
        {
            return new SchemaDefinitionNode(
                Location, Directives, OperationTypes);
        }

        public SchemaDefinitionNode WithDirectives(
            IReadOnlyCollection<DirectiveNode> directives)
        {
            return new SchemaDefinitionNode(
                Location, directives, OperationTypes);
        }

        public SchemaDefinitionNode WithOperationTypes(
            IReadOnlyCollection<OperationTypeDefinitionNode> operationTypes)
        {
            return new SchemaDefinitionNode(
                Location, Directives, operationTypes);
        }
    }
}

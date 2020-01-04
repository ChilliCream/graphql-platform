using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public sealed class SchemaDefinitionNode
        : SchemaDefinitionNodeBase
        , ITypeSystemDefinitionNode
    {
        public SchemaDefinitionNode(
            Location location,
            StringValueNode? description,
            IReadOnlyList<DirectiveNode> directives,
            IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
            : base(location, directives, operationTypes)
        {
            Description = description;
        }

        public override NodeKind Kind { get; } = NodeKind.SchemaDefinition;

        public StringValueNode? Description { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            if (Description is { })
            {
                yield return Description;
            }

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
            }

            foreach (OperationTypeDefinitionNode operationType in OperationTypes)
            {
                yield return operationType;
            }
        }

        public SchemaDefinitionNode WithLocation(Location location)
        {
            return new SchemaDefinitionNode(
                Location, Description, Directives, OperationTypes);
        }

        public SchemaDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new SchemaDefinitionNode(
                Location, Description, directives, OperationTypes);
        }

        public SchemaDefinitionNode WithOperationTypes(
            IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
        {
            return new SchemaDefinitionNode(
                Location, Description, Directives, operationTypes);
        }

        public SchemaDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new SchemaDefinitionNode(
                Location, description, Directives, OperationTypes);
        }
    }
}

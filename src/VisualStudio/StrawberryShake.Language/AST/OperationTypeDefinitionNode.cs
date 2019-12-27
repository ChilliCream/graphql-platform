using System;

namespace HotChocolate.Language
{
    public sealed class OperationTypeDefinitionNode
        : ISyntaxNode
    {
        public OperationTypeDefinitionNode(
            Location? location,
            OperationType operation,
            NamedTypeNode type)
        {
            Location = location;
            Operation = operation;
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public NodeKind Kind { get; } = NodeKind.OperationTypeDefinition;

        public Location? Location { get; }

        public OperationType Operation { get; }

        public NamedTypeNode Type { get; }

        public OperationTypeDefinitionNode WithLocation(Location? location)
        {
            return new OperationTypeDefinitionNode(
                location, Operation, Type);
        }

        public OperationTypeDefinitionNode WithOperation(OperationType operation)
        {
            return new OperationTypeDefinitionNode(
                Location, operation, Type);
        }

        public OperationTypeDefinitionNode WithType(NamedTypeNode type)
        {
            return new OperationTypeDefinitionNode(
                Location, Operation, type);
        }
    }
}

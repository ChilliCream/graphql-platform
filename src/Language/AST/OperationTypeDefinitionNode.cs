using System;

namespace HotChocolate.Language
{
    public sealed class OperationTypeDefinitionNode
        : ISyntaxNode
    {
        public OperationTypeDefinitionNode(
            Location location,
            OperationType operation,
            NamedTypeNode type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Location = location;
            Operation = operation;
            Type = type;
        }

        public NodeKind Kind { get; } = NodeKind.OperationTypeDefinition;

        public Location Location { get; }

        public OperationType Operation { get; }

        public NamedTypeNode Type { get; }
    }
}

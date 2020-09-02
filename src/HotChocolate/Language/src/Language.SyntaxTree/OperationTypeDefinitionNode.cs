using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

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

        public SyntaxKind Kind { get; } = SyntaxKind.OperationTypeDefinition;

        public Location? Location { get; }

        public OperationType Operation { get; }

        public NamedTypeNode Type { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Type;
        }

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public override string ToString() => SyntaxPrinter.Print(this, true);

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <param name="indented">
        /// A value that indicates whether the GraphQL output should be formatted,
        /// which includes indenting nested GraphQL tokens, adding
        /// new lines, and adding white space between property names and values.
        /// </param>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

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

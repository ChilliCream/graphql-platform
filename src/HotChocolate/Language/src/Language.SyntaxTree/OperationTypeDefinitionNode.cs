using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represents the operation type definition syntax.
/// <code>
/// schema { query: Query }
/// </code>
/// </summary>
public sealed class OperationTypeDefinitionNode : ISyntaxNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="operation">
    /// The GraphQL operation.
    /// </param>
    /// <param name="type">
    /// The GraphQL type that represents the operation.
    /// </param>
    public OperationTypeDefinitionNode(
        Location? location,
        OperationType operation,
        NamedTypeNode type)
    {
        Location = location;
        Operation = operation;
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.OperationTypeDefinition;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// Gets the GraphQL operation.
    /// </summary>
    public OperationType Operation { get; }

    /// <summary>
    /// Gets the GraphQL operation type.
    /// </summary>
    public NamedTypeNode Type { get; }

    /// <inheritdoc />
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

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Location" /> with <paramref name="location" />.
    /// </summary>
    /// <param name="location">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="location" />.
    /// </returns>
    public OperationTypeDefinitionNode WithLocation(Location? location)
        => new(location, Operation, Type);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Operation" /> with <paramref name="operation" />.
    /// </summary>
    /// <param name="operation">
    /// The operation that shall be used to replace the current operation.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="operation" />.
    /// </returns>
    public OperationTypeDefinitionNode WithOperation(OperationType operation)
        => new(Location, operation, Type);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Type" /> with <paramref name="type" />.
    /// </summary>
    /// <param name="type">
    /// The type that shall be used to replace the current type.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="type" />.
    /// </returns>
    public OperationTypeDefinitionNode WithType(NamedTypeNode type)
        => new(Location, Operation, type);
}

using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class OperationTypeDefinitionNode
    : ISyntaxNode
    , IEquatable<OperationTypeDefinitionNode>
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

    public SyntaxKind Kind => SyntaxKind.OperationTypeDefinition;

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

    public OperationTypeDefinitionNode WithLocation(Location? location) =>
        new(location, Operation, Type);

    public OperationTypeDefinitionNode WithOperation(OperationType operation) =>
        new(Location, operation, Type);

    public OperationTypeDefinitionNode WithType(NamedTypeNode type) =>
        new(Location, Operation, type);

    public bool Equals(OperationTypeDefinitionNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Operation == other.Operation && Type.Equals(other.Type);
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) ||
                                                obj is OperationTypeDefinitionNode other &&
                                                Equals(other);

    public override int GetHashCode() => HashCode.Combine((int)Operation, Type, Kind);
}

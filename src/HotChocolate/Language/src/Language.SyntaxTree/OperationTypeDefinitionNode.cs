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

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.OperationTypeDefinition;

    /// <inheritdoc />
    public Location? Location { get; }

    public OperationType Operation { get; }

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

    public OperationTypeDefinitionNode WithOperation(OperationType operation)
        => new(Location, operation, Type);

    public OperationTypeDefinitionNode WithType(NamedTypeNode type)
        => new(Location, Operation, type);

    /// <summary>
    /// Determines whether the specified <see cref="OperationTypeDefinitionNode"/>
    /// is equal to the current <see cref="OperationTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="OperationTypeDefinitionNode"/> to compare with the current
    /// <see cref="OperationTypeDefinitionNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="OperationTypeDefinitionNode"/> is equal
    /// to the current <see cref="OperationTypeDefinitionNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
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

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to
    /// the current <see cref="OperationTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current
    /// <see cref="OperationTypeDefinitionNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to the
    /// current <see cref="OperationTypeDefinitionNode"/>; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) ||
            (obj is OperationTypeDefinitionNode other && Equals(other));

    /// <summary>
    /// Serves as a hash function for a <see cref="OperationTypeDefinitionNode"/>
    /// object.
    /// </summary>
    /// <returns>
    /// A hash code for this instance that is suitable for use in
    /// hashing algorithms and data structures such as a hash table.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(Kind, Operation, Type);

    /// <summary>
    /// The equal operator.
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> are equal.
    /// </returns>
    public static bool operator ==(
        OperationTypeDefinitionNode? left,
        OperationTypeDefinitionNode? right)
        => Equals(left, right);

    /// <summary>
    /// The not equal operator.
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> are not equal.
    /// </returns>
    public static bool operator !=(
        OperationTypeDefinitionNode? left,
        OperationTypeDefinitionNode? right)
        => !Equals(left, right);
}

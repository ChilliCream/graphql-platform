using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// Represents GraphQL schema definition syntax.
/// </para>
/// <para>
/// A GraphQL service’s collective type system capabilities
/// are referred to as that service’s “schema”.
/// </para>
/// <para>
/// A schema is defined in terms of the types and directives it supports as well
/// as the root operation types for each kind of operation: query, mutation, and subscription;
/// this determines the place in the type system where those operations begin.
/// </para>
/// </summary>
public sealed class SchemaDefinitionNode
    : SchemaDefinitionNodeBase
    , ITypeSystemDefinitionNode
    , IEquatable<SchemaDefinitionNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="SchemaDefinitionNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="description">
    /// The description of the schema.
    /// </param>
    /// <param name="directives">
    /// The applied directives.
    /// </param>
    /// <param name="operationTypes">
    /// The operation types.
    /// </param>
    public SchemaDefinitionNode(
        Location? location,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
        : base(location, directives, operationTypes)
    {
        Description = description;
    }

    /// <inheritdoc cref="SchemaDefinitionNodeBase.Kind"/>
    public override SyntaxKind Kind => SyntaxKind.SchemaDefinition;

    /// <summary>
    /// Gets the schema description.
    /// </summary>
    public StringValueNode? Description { get; }

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Description is not null)
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
    public SchemaDefinitionNode WithLocation(Location? location)
        => new(location, Description, Directives, OperationTypes);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Description" /> with <paramref name="description" />.
    /// </summary>
    /// <param name="description">
    /// The description that shall be used to replace the current description.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="description" />.
    /// </returns>
    public SchemaDefinitionNode WithDescription(
        StringValueNode? description)
        => new(Location, description, Directives, OperationTypes);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="NamedSyntaxNode.Directives" /> with <paramref name="directives" />.
    /// </summary>
    /// <param name="directives">
    /// The directives that shall be used to replace the current
    /// <see cref="NamedSyntaxNode.Directives" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="directives" />.
    /// </returns>
    public SchemaDefinitionNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
        => new(Location, Description, directives, OperationTypes);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="SchemaDefinitionNodeBase.OperationTypes" /> with
    /// <paramref name="operationTypes" />.
    /// </summary>
    /// <param name="operationTypes">
    /// The operationTypes that shall be used to replace the current
    /// <see cref="SchemaDefinitionNodeBase.OperationTypes" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="operationTypes" />.
    /// </returns>
    public SchemaDefinitionNode WithOperationTypes(
        IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
        => new(Location, Description, Directives, operationTypes);

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">
    /// An object to compare with this object.
    /// </param>
    /// <returns>
    /// true if the current object is equal to the <paramref name="other" /> parameter;
    /// otherwise, false.
    /// </returns>
    public bool Equals(SchemaDefinitionNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return base.Equals(other)
           && Description.IsEqualTo(other.Description);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with the current object.
    /// </param>
    /// <returns>
    /// true if the specified object  is equal to the current object; otherwise, false.
    /// </returns>
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) ||
            (obj is SchemaDefinitionNode other && Equals(other));

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), Kind, Description);

    /// <summary>
    /// The equal operator.
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> are equal.
    /// </returns>
    public static bool operator ==(
        SchemaDefinitionNode? left,
        SchemaDefinitionNode? right)
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
        SchemaDefinitionNode? left,
        SchemaDefinitionNode? right)
        => !Equals(left, right);
}

using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class SchemaDefinitionNode
    : SchemaDefinitionNodeBase
    , ITypeSystemDefinitionNode
    , IEquatable<SchemaDefinitionNode>
{
    public SchemaDefinitionNode(
        Location? location,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
        : base(location, directives, operationTypes)
    {
        Description = description;
    }

    public override SyntaxKind Kind { get; } = SyntaxKind.SchemaDefinition;

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

    public SchemaDefinitionNode WithLocation(Location? location)
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
               && Kind == other.Kind
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
    {
        return ReferenceEquals(this, obj) || obj is SchemaDefinitionNode other
            && Equals(other);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(base.GetHashCode());
        hashCode.AddRange((int)Kind, Description);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(
        SchemaDefinitionNode? left,
        SchemaDefinitionNode? right)
        => Equals(left, right);

    public static bool operator !=(
        SchemaDefinitionNode? left,
        SchemaDefinitionNode? right)
        => !Equals(left, right);
}

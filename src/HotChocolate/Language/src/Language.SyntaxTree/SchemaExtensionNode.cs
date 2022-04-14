using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class SchemaExtensionNode
    : SchemaDefinitionNodeBase
    , ITypeSystemExtensionNode
    , IEquatable<SchemaExtensionNode>
{
    public SchemaExtensionNode(
        Location? location,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
        : base(location, directives, operationTypes)
    {
    }

    public override SyntaxKind Kind { get; } = SyntaxKind.SchemaExtension;

    public IEnumerable<ISyntaxNode> GetNodes()
    {
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

    public SchemaExtensionNode WithLocation(Location? location)
    {
        return new SchemaExtensionNode(
            location, Directives, OperationTypes);
    }

    public SchemaExtensionNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
    {
        return new SchemaExtensionNode(
            Location, directives, OperationTypes);
    }

    public SchemaExtensionNode WithOperationTypes(
        IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
    {
        return new SchemaExtensionNode(
            Location, Directives, operationTypes);
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
    public bool Equals(SchemaExtensionNode? other)
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
               && Kind == other.Kind;
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
        return ReferenceEquals(this, obj) || obj is SchemaExtensionNode other
            && Equals(other);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), (int)Kind);

    public static bool operator ==(
        SchemaExtensionNode? left,
        SchemaExtensionNode? right)
        => Equals(left, right);

    public static bool operator !=(
        SchemaExtensionNode? left,
        SchemaExtensionNode? right)
        => !Equals(left, right);
}

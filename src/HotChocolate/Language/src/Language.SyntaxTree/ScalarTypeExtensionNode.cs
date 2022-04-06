using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class ScalarTypeExtensionNode
    : ScalarTypeDefinitionNodeBase
    , ITypeExtensionNode
    , IEquatable<ScalarTypeExtensionNode>
{
    public ScalarTypeExtensionNode(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives)
        : base(location, name, directives)
    {
    }

    public override SyntaxKind Kind { get; } = SyntaxKind.ScalarTypeExtension;

    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;

        foreach (DirectiveNode directive in Directives)
        {
            yield return directive;
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
    public override string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

    public ScalarTypeExtensionNode WithLocation(Location? location)
    {
        return new ScalarTypeExtensionNode(
            location, Name, Directives);
    }

    public ScalarTypeExtensionNode WithName(NameNode name)
    {
        return new ScalarTypeExtensionNode(
            Location, name, Directives);
    }

    public ScalarTypeExtensionNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
    {
        return new ScalarTypeExtensionNode(
            Location, Name, directives);
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
    public bool Equals(ScalarTypeExtensionNode? other)
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
        return ReferenceEquals(this, obj) || obj is ScalarTypeExtensionNode other
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
        ScalarTypeExtensionNode? left,
        ScalarTypeExtensionNode? right)
        => Equals(left, right);

    public static bool operator !=(
        ScalarTypeExtensionNode? left,
        ScalarTypeExtensionNode? right)
        => !Equals(left, right);
}

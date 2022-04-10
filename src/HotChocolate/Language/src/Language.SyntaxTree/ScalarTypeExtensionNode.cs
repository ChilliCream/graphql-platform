using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Scalar type extensions are used to represent a scalar type which has been
/// extended from some original scalar type. For example, this might be used
/// by a GraphQL tool or service which adds directives to an existing scalar.
/// </summary>
public sealed class ScalarTypeExtensionNode
    : NamedSyntaxNode
    , ITypeExtensionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="ScalarTypeExtensionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the scalar.
    /// </param>
    /// <param name="description">
    /// The description of the scalar.
    /// </param>
    /// <param name="directives">
    /// The applied directives.
    /// </param>
    public ScalarTypeExtensionNode(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives)
        : base(location, name, directives)
    {
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.ScalarTypeExtension;

    /// <inheritdoc />
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
    public ScalarTypeExtensionNode WithLocation(Location? location)
        => new(location, Name, Directives);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Name" /> with <paramref name="name" />.
    /// </summary>
    /// <param name="name">
    /// The name that shall be used to replace the current name.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="name" />.
    /// </returns>
    public ScalarTypeExtensionNode WithName(NameNode name)
        => new(Location, name, Directives);

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
    public ScalarTypeExtensionNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, directives);

    /// <summary>
    /// Determines whether the specified <see cref="ScalarTypeExtensionNode"/>
    /// is equal to the current <see cref="ScalarTypeExtensionNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="ScalarTypeExtensionNode"/> to compare with the current
    /// <see cref="ScalarTypeExtensionNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="ScalarTypeExtensionNode"/> is equal
    /// to the current <see cref="ScalarTypeExtensionNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(ScalarTypeExtensionNode? other) => base.Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to
    /// the current <see cref="ScalarTypeExtensionNode"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current
    /// <see cref="ScalarTypeExtensionNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to the
    /// current <see cref="ScalarTypeExtensionNode"/>; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
        => Equals(obj as ScalarTypeExtensionNode);

    /// <summary>
    /// Serves as a hash function for a <see cref="ScalarTypeExtensionNode"/>
    /// object.
    /// </summary>
    /// <returns>
    /// A hash code for this instance that is suitable for use in
    /// hashing algorithms and data structures such as a hash table.
    /// </returns>
    public override int GetHashCode() => base.GetHashCode();

    /// <summary>
    /// The equal operator.
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> are equal.
    /// </returns>
    public static bool operator ==(
        ScalarTypeExtensionNode? left,
        ScalarTypeExtensionNode? right)
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
        ScalarTypeExtensionNode? left,
        ScalarTypeExtensionNode? right)
        => !Equals(left, right);
}

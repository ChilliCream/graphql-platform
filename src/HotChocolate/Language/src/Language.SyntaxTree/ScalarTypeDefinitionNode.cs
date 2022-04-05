using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class ScalarTypeDefinitionNode
    : ScalarTypeDefinitionNodeBase
    , ITypeDefinitionNode
    , IEquatable<ScalarTypeDefinitionNode>
{
    public ScalarTypeDefinitionNode(
        Location? location,
        NameNode name,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives)
        : base(location, name, directives)
    {
        Description = description;
    }

    public override SyntaxKind Kind { get; } = SyntaxKind.ScalarTypeDefinition;

    public StringValueNode? Description { get; }

    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Description is { })
        {
            yield return Description;
        }

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

    public ScalarTypeDefinitionNode WithLocation(Location? location)
    {
        return new ScalarTypeDefinitionNode(
            location, Name, Description,
            Directives);
    }

    public ScalarTypeDefinitionNode WithName(NameNode name)
    {
        return new ScalarTypeDefinitionNode(
            Location, name, Description,
            Directives);
    }

    public ScalarTypeDefinitionNode WithDescription(
        StringValueNode? description)
    {
        return new ScalarTypeDefinitionNode(
            Location, Name, description,
            Directives);
    }

    public ScalarTypeDefinitionNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
    {
        return new ScalarTypeDefinitionNode(
            Location, Name, Description,
            directives);
    }

    /// <summary>
    /// Determines whether the specified <see cref="ScalarTypeDefinitionNode"/>
    /// is equal to the current <see cref="ScalarTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="ScalarTypeDefinitionNode"/> to compare with the current
    /// <see cref="ScalarTypeDefinitionNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="ScalarTypeDefinitionNode"/> is equal
    /// to the current <see cref="ScalarTypeDefinitionNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(ScalarTypeDefinitionNode? other)
    {
        return base.Equals(other) && Description.IsEqualTo(other.Description);
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to
    /// the current <see cref="ScalarTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current
    /// <see cref="ScalarTypeDefinitionNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to the
    /// current <see cref="ScalarTypeDefinitionNode"/>; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
        => Equals(obj as ScalarTypeDefinitionNode);

    /// <summary>
    /// Serves as a hash function for a <see cref="ScalarTypeDefinitionNode"/>
    /// object.
    /// </summary>
    /// <returns>
    /// A hash code for this instance that is suitable for use in
    /// hashing algorithms and data structures such as a hash table.
    /// </returns>
    public override int GetHashCode()
       => HashCode.Combine(base.GetHashCode(), Description?.GetHashCode());

    public static bool operator ==(
        ScalarTypeDefinitionNode? left,
        ScalarTypeDefinitionNode? right)
        => Equals(left, right);

    public static bool operator !=(
        ScalarTypeDefinitionNode? left,
        ScalarTypeDefinitionNode? right)
        => !Equals(left, right);
}

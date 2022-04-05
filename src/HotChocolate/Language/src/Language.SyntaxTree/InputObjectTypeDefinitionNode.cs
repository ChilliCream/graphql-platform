using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class InputObjectTypeDefinitionNode
    : InputObjectTypeDefinitionNodeBase
    , ITypeDefinitionNode
    , IEquatable<InputObjectTypeDefinitionNode>
{
    public InputObjectTypeDefinitionNode(
        Location? location,
        NameNode name,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<InputValueDefinitionNode> fields)
        : base(location, name, directives, fields)
    {
        Description = description;
    }

    public override SyntaxKind Kind => SyntaxKind.InputObjectTypeDefinition;

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

        foreach (InputValueDefinitionNode field in Fields)
        {
            yield return field;
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

    public InputObjectTypeDefinitionNode WithLocation(Location? location)
    {
        return new InputObjectTypeDefinitionNode(
            location, Name, Description,
            Directives, Fields);
    }

    public InputObjectTypeDefinitionNode WithName(NameNode name)
    {
        return new InputObjectTypeDefinitionNode(
            Location, name, Description,
            Directives, Fields);
    }

    public InputObjectTypeDefinitionNode WithDescription(
        StringValueNode? description)
    {
        return new InputObjectTypeDefinitionNode(
            Location, Name, description,
            Directives, Fields);
    }

    public InputObjectTypeDefinitionNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
    {
        return new InputObjectTypeDefinitionNode(
            Location, Name, Description,
            directives, Fields);
    }

    public InputObjectTypeDefinitionNode WithFields(
        IReadOnlyList<InputValueDefinitionNode> fields)
    {
        return new InputObjectTypeDefinitionNode(
            Location, Name, Description,
            Directives, fields);
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
    public bool Equals(InputObjectTypeDefinitionNode? other)
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
    {
        return ReferenceEquals(this, obj) || obj is InputObjectTypeDefinitionNode other
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
        return HashCode.Combine(base.GetHashCode(), Description);
    }

    public static bool operator ==(
        InputObjectTypeDefinitionNode? left,
        InputObjectTypeDefinitionNode? right)
        => Equals(left, right);

    public static bool operator !=(
        InputObjectTypeDefinitionNode? left,
        InputObjectTypeDefinitionNode? right)
        => !Equals(left, right);
}

using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represents a possible value of a GraphQL enum type.
/// </summary>
public sealed class EnumValueDefinitionNode
    : NamedSyntaxNode
    , IEquatable<EnumValueDefinitionNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="EnumValueDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name that this syntax node holds.
    /// </param>
    /// <param name="description">
    /// The description of the directive.
    /// </param>
    /// <param name="directives">
    /// The applied directives.
    /// </param>
    public EnumValueDefinitionNode(
        Location? location,
        NameNode name,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives)
        : base(location, name, directives)
    {
        Description = description;
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.EnumValueDefinition;

    /// <summary>
    /// Gets the description of this enum value.
    /// </summary>
    public StringValueNode? Description { get; }

    /// <inheritdoc />
    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Description is not null)
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
    public EnumValueDefinitionNode WithLocation(Location? location)
        => new(location, Name, Description, Directives);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="NamedSyntaxNode.Name" /> with <paramref name="name" />.
    /// </summary>
    /// <param name="name">
    /// The name that shall be used to replace the current name.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="name" />.
    /// </returns>
    public EnumValueDefinitionNode WithName(NameNode name)
        => new(Location, name, Description, Directives);

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
    public EnumValueDefinitionNode WithDescription(StringValueNode? description)
        => new(Location, Name, description, Directives);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="NamedSyntaxNode.Directives" /> with <paramref name="directives" />.
    /// </summary>
    /// <param name="directives">
    /// The directives that shall be used to replace the current directives.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="directives" />.
    /// </returns>
    public EnumValueDefinitionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, Description, directives);

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
    public bool Equals(EnumValueDefinitionNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return base.Equals(other) && Equals(Description, other.Description);
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
            (obj is EnumValueDefinitionNode other && Equals(other));

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * 397) ^ Kind.GetHashCode();
            return (hashCode * 397) ^ (Description?.GetHashCode() ?? 0);
        }
    }

    /// <summary>
    /// The equal operator.
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> are equal.
    /// </returns>
    public static bool operator ==(EnumValueDefinitionNode? left, EnumValueDefinitionNode? right)
        => Equals(left, right);


    /// <summary>
    /// The not equal operator.
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> are not equal.
    /// </returns>
    public static bool operator !=(EnumValueDefinitionNode? left, EnumValueDefinitionNode? right)
        => !Equals(left, right);
}

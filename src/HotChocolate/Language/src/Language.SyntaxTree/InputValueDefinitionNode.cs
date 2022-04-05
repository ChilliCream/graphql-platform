using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// A GraphQL Input Object defines a set of input fields; the input fields are either
/// scalars, enums, or other input objects. This allows arguments to accept arbitrarily
/// complex structs.
/// https://graphql.github.io/graphql-spec/June2018/#sec-Input-Objects
/// </summary>
public sealed class InputValueDefinitionNode : NamedSyntaxNode, IEquatable<InputValueDefinitionNode>
{
    public InputValueDefinitionNode(
        Location? location,
        NameNode name,
        StringValueNode? description,
        ITypeNode type,
        IValueNode? defaultValue,
        IReadOnlyList<DirectiveNode> directives)
        : base(location, name, directives)
    {
        Description = description;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        DefaultValue = defaultValue;
    }

    public override SyntaxKind Kind => SyntaxKind.InputValueDefinition;

    public StringValueNode? Description { get; }

    public ITypeNode Type { get; }

    public IValueNode? DefaultValue { get; }

    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Description is { })
        {
            yield return Description;
        }

        yield return Name;
        yield return Type;

        if (DefaultValue is { })
        {
            yield return DefaultValue;
        }

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

    public InputValueDefinitionNode WithLocation(Location? location)
        => new(location, Name, Description, Type, DefaultValue, Directives);

    public InputValueDefinitionNode WithName(NameNode name)
        => new(Location, name, Description, Type, DefaultValue, Directives);

    public InputValueDefinitionNode WithDescription(StringValueNode? description)
        => new(Location, Name, description, Type, DefaultValue, Directives);

    public InputValueDefinitionNode WithType(ITypeNode type)
        => new(Location, Name, Description, type, DefaultValue, Directives);

    public InputValueDefinitionNode WithDefaultValue(IValueNode defaultValue)
        => new(Location, Name, Description, Type, defaultValue, Directives);

    public InputValueDefinitionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, Description, Type, DefaultValue, directives);

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">
    /// An object to compare with this object.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the current object is equal to the
    /// <paramref name="other" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(InputValueDefinitionNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return base.Equals(other) &&
            Equals(Description, other.Description) &&
            Type.Equals(other.Type) &&
            Equals(DefaultValue, other.DefaultValue);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with the current object.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified object  is equal to the current object;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) ||
            obj is InputValueDefinitionNode other &&
            Equals(other);

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
            hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ Type.GetHashCode();
            hashCode = (hashCode * 397) ^ (DefaultValue != null ? DefaultValue.GetHashCode() : 0);
            return hashCode;
        }
    }

    public static bool operator ==(InputValueDefinitionNode? left, InputValueDefinitionNode? right)
        => Equals(left, right);

    public static bool operator !=(InputValueDefinitionNode? left, InputValueDefinitionNode? right)
        => !Equals(left, right);
}

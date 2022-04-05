using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// The base class for enum type definitions.
/// </summary>
public abstract class EnumTypeDefinitionNodeBase
    : NamedSyntaxNode
    , IEquatable<EnumTypeDefinitionNodeBase>
{
    /// <summary>
    /// Initializes a new instance of
    /// <see cref="EnumTypeDefinitionNodeBase"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The enum type name.
    /// </param>
    /// <param name="directives">
    /// The directives applied to the enum type.
    /// </param>
    /// <param name="values">
    /// The enum values.
    /// </param>
    protected EnumTypeDefinitionNodeBase(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<EnumValueDefinitionNode> values)
        : base(location, name, directives)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
    }

    /// <summary>
    /// Gets the enum values.
    /// </summary>
    public IReadOnlyList<EnumValueDefinitionNode> Values { get; }

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
    public bool Equals(EnumTypeDefinitionNodeBase? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return base.Equals(other) &&
            EqualityHelper.Equals(Values, other.Values);
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
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((EnumTypeDefinitionNodeBase) obj);
    }

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
            return (base.GetHashCode() * 397) ^ EqualityHelper.GetHashCode(Values);
        }
    }

    public static bool operator ==(
        EnumTypeDefinitionNodeBase? left,
        EnumTypeDefinitionNodeBase? right)
        => Equals(left, right);

    public static bool operator !=(
        EnumTypeDefinitionNodeBase? left,
        EnumTypeDefinitionNodeBase? right)
        => !Equals(left, right);
}

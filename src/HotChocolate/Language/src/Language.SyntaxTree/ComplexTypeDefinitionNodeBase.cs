using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// The base class for complex type definitions e.g. interface or object
/// </summary>
public abstract class ComplexTypeDefinitionNodeBase
    : NamedSyntaxNode
    , IEquatable<ComplexTypeDefinitionNodeBase>
{
    /// <summary>
    /// Initializes a new instance of
    /// <see cref="ComplexTypeDefinitionNodeBase"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the named syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name that this syntax node holds.
    /// </param>
    /// <param name="directives">
    /// The directives that are annotated to this syntax node.
    /// </param>
    /// <param name="interfaces">
    /// The interfaces that this type implements.
    /// </param>
    /// <param name="fields">
    /// The fields that this type exposes.
    /// </param>
    protected ComplexTypeDefinitionNodeBase(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<NamedTypeNode> interfaces,
        IReadOnlyList<FieldDefinitionNode> fields)
        : base(location, name, directives)
    {
        Interfaces = interfaces ?? throw new ArgumentNullException(nameof(interfaces));
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
    }

    /// <summary>
    /// Gets the interfaces that this type implements.
    /// </summary>
    public IReadOnlyList<NamedTypeNode> Interfaces { get; }

    /// <summary>
    /// Gets the fields that this type exposes.
    /// </summary>
    public IReadOnlyList<FieldDefinitionNode> Fields { get; }

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
    public bool Equals(ComplexTypeDefinitionNodeBase? other)
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
            EqualityHelper.Equals(Interfaces, other.Interfaces) &&
            EqualityHelper.Equals(Fields, other.Fields);
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

        return Equals((ComplexTypeDefinitionNodeBase)obj);
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
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * 397) ^ Kind.GetHashCode();
            hashCode = (hashCode * 397) ^ EqualityHelper.GetHashCode(Interfaces);
            hashCode = (hashCode * 397) ^ EqualityHelper.GetHashCode(Fields);
            return hashCode;
        }
    }

    public static bool operator ==(
        ComplexTypeDefinitionNodeBase? left,
        ComplexTypeDefinitionNodeBase? right)
        => Equals(left, right);

    public static bool operator !=(
        ComplexTypeDefinitionNodeBase? left,
        ComplexTypeDefinitionNodeBase? right)
        => !Equals(left, right);
}

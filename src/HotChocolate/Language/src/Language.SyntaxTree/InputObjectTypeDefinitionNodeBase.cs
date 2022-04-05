using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

/// <summary>
/// The base class for input object types and input object type extensions.
/// </summary>
public abstract class InputObjectTypeDefinitionNodeBase
    : NamedSyntaxNode
    , IEquatable<InputObjectTypeDefinitionNodeBase>
{
    /// <summary>
    /// Initializes a new instance of <see cref="InputObjectTypeDefinitionNodeBase"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the input object type.
    /// </param>
    /// <param name="directives">
    /// The directives of the input object type.
    /// </param>
    /// <param name="fields">
    /// The input fields of the input object type.
    /// </param>
    protected InputObjectTypeDefinitionNodeBase(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<InputValueDefinitionNode> fields)
        : base(location, name, directives)
    {
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
    }

    /// <summary>
    /// Gets the input fields.
    /// </summary>
    public IReadOnlyList<InputValueDefinitionNode> Fields { get; }

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
    public bool Equals(InputObjectTypeDefinitionNodeBase? other)
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
               && Fields.Equals(other.Fields);
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
        if (ReferenceEquals(null, obj))
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

        return Equals((InputObjectTypeDefinitionNodeBase)obj);
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
        HashCodeExtensions.AddNodes(ref hashCode, Fields);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(
        InputObjectTypeDefinitionNodeBase? left,
        InputObjectTypeDefinitionNodeBase? right)
        => Equals(left, right);

    public static bool operator !=(
        InputObjectTypeDefinitionNodeBase? left,
        InputObjectTypeDefinitionNodeBase? right)
        => !Equals(left, right);
}

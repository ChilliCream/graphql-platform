using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

public abstract class InputObjectTypeDefinitionNodeBase
    : NamedSyntaxNode
    , IEquatable<InputObjectTypeDefinitionNodeBase>
{
    protected InputObjectTypeDefinitionNodeBase(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<InputValueDefinitionNode> fields)
        : base(location, name, directives)
    {
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
    }

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
        return HashCode.Combine(base.GetHashCode(), Fields);
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

using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

public abstract class ScalarTypeDefinitionNodeBase
    : NamedSyntaxNode
    , IEquatable<ScalarTypeDefinitionNodeBase>
{
    protected ScalarTypeDefinitionNodeBase(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives)
        : base(location, name, directives)
    { }

    /// <summary>
    /// Determines whether the specified <see cref="ScalarTypeDefinitionNodeBase"/>
    /// is equal to the current <see cref="ScalarTypeDefinitionNodeBase"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="ScalarTypeDefinitionNodeBase"/> to compare with the current
    /// <see cref="ScalarTypeDefinitionNodeBase"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="ScalarTypeDefinitionNodeBase"/> is equal
    /// to the current <see cref="ScalarTypeDefinitionNodeBase"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(ScalarTypeDefinitionNodeBase? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name.IsEqualTo(other.Name) && Directives.IsEqualTo(other.Directives);
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to
    /// the current <see cref="ScalarTypeDefinitionNodeBase"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current
    /// <see cref="ScalarTypeDefinitionNodeBase"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to the
    /// current <see cref="ScalarTypeDefinitionNodeBase"/>; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
        => Equals(obj as ScalarTypeDefinitionNodeBase);

    /// <summary>
    /// Serves as a hash function for a <see cref="ScalarTypeDefinitionNodeBase"/>
    /// object.
    /// </summary>
    /// <returns>
    /// A hash code for this instance that is suitable for use in
    /// hashing algorithms and data structures such as a hash table.
    /// </returns>
    public override int GetHashCode()
       => HashCode.Combine(Kind, Name?.GetHashCode(), Directives?.GetHashCode());

    public static bool operator ==(
        ScalarTypeDefinitionNodeBase? left,
        ScalarTypeDefinitionNodeBase? right)
        => Equals(left, right);

    public static bool operator !=(
        ScalarTypeDefinitionNodeBase? left,
        ScalarTypeDefinitionNodeBase? right)
        => !Equals(left, right);
}

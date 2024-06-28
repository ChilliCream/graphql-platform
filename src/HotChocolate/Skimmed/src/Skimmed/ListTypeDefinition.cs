using HotChocolate.Types;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL list type definition.
/// </summary>
public sealed class ListTypeDefinition(ITypeDefinition elementType) : ITypeDefinition
{
    /// <inheritdoc />
    public TypeKind Kind => TypeKind.List;

    /// <summary>
    /// Gets the element type of the list.
    /// </summary>
    public ITypeDefinition ElementType { get; } = elementType ??
        throw new ArgumentNullException(nameof(elementType));

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => RewriteTypeRef(this).ToString(true);

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other)
        => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is ListTypeDefinition otherList &&
            ElementType.Equals(otherList.ElementType, comparison);
    }
}

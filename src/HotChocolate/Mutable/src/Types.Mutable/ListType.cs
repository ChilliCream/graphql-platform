using HotChocolate.Language;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a GraphQL list type definition.
/// </summary>
public sealed class ListType : IListType
{
    /// <summary>
    /// Represents a GraphQL list type definition.
    /// </summary>
    public ListType(IType elementType)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        ElementType = elementType;
    }

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.List;

    /// <summary>
    /// Gets the element type of the list.
    /// </summary>
    public IType ElementType { get; }

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => FormatTypeRef(this).ToString(true);

    public ISyntaxNode ToSyntaxNode()
        => FormatTypeRef(this);

    /// <inheritdoc />
    public bool Equals(IType? other)
        => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is ListType otherList &&
            ElementType.Equals(otherList.ElementType, comparison);
    }
}

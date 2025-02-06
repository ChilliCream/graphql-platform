using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL list type definition.
/// </summary>
public sealed class ListTypeDefinition : ITypeDefinition, IReadOnlyWrapperType
{
    /// <summary>
    /// Represents a GraphQL list type definition.
    /// </summary>
    public ListTypeDefinition(ITypeDefinition elementType)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        ElementType = elementType;
    }

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.List;

    /// <summary>
    /// Gets the element type of the list.
    /// </summary>
    public ITypeDefinition ElementType { get; }

    IReadOnlyTypeDefinition IReadOnlyWrapperType.Type => ElementType;

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => RewriteTypeRef(this).ToString(true);

    public ISyntaxNode ToSyntaxNode()
        => RewriteTypeRef(this);

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

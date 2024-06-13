using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class ListType : ITypeDefinition
{
    public ListType(ITypeDefinition elementType)
    {
        ElementType = elementType ??
            throw new ArgumentNullException(nameof(elementType));
    }

    public TypeKind Kind => TypeKind.List;

    public ITypeDefinition ElementType { get; }

    public override string ToString()
        => RewriteTypeRef(this).ToString(true);

    public bool Equals(ITypeDefinition? other)
        => Equals(other, TypeComparison.Reference);

    public bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is ListType otherList &&
            ElementType.Equals(otherList.ElementType, comparison);
    }
}

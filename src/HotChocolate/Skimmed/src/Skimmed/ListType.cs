using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class ListType(IType elementType) : IType
{
    public TypeKind Kind => TypeKind.List;

    public IType ElementType { get; } = elementType ??
        throw new ArgumentNullException(nameof(elementType));

    public override string ToString()
        => RewriteTypeRef(this).ToString(true);
    
    public bool Equals(IType? other)
        => Equals(other, TypeComparison.Reference);
    
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

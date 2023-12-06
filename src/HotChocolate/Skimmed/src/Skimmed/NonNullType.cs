using static HotChocolate.Skimmed.Properties.SkimmedResources;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class NonNullType : IType
{
    public NonNullType(IType nullableType)
    {
        if (nullableType is null)
        {
            throw new ArgumentNullException(nameof(nullableType));
        }

        if (nullableType.Kind is TypeKind.NonNull)
        {
            throw new ArgumentException(NonNullType_InnerTypeCannotBeNonNull, nameof(nullableType));
        }

        NullableType = nullableType;
    }

    public TypeKind Kind => TypeKind.NonNull;
    
    public IType NullableType { get; }
    
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
        
        return other is NonNullType otherNonNull && 
            NullableType.Equals(otherNonNull.NullableType, comparison);
    }
}

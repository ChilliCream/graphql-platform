namespace HotChocolate.Skimmed;

public sealed class NotSetType : IType
{
    private NotSetType()
    {
    }

    public TypeKind Kind => TypeKind.Scalar;
    
    public bool Equals(IType? other)
        => Equals(other, TypeComparison.Reference);
    
    public bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }
        
        return other is NotSetType;
    }

    public static readonly NotSetType Default = new();
}

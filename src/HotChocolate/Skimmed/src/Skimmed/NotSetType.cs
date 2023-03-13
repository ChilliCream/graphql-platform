namespace HotChocolate.Skimmed;

public sealed class NotSetType : IType
{
    private NotSetType()
    {
    }

    public TypeKind Kind => TypeKind.Scalar;

    public static readonly NotSetType Default = new();
}

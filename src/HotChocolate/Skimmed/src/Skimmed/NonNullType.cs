namespace HotChocolate.Skimmed;

public sealed class NonNullType : IType
{
    public NonNullType(IType innerType)
    {
        if (innerType is null)
        {
            throw new ArgumentNullException(nameof(innerType));
        }

        if (innerType.Kind is TypeKind.NonNull)
        {
            throw new ArgumentException(
                "The inner type cannot be a non-null type.",
                nameof(innerType));
        }

        InnerType = innerType;
    }

    public TypeKind Kind => TypeKind.NonNull;

    public IType InnerType { get; }
}

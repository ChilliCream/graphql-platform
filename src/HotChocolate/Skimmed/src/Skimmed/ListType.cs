namespace HotChocolate.Skimmed;

public sealed class ListType : IType
{
    public ListType(IType elementType)
    {
        ElementType = elementType ??
            throw new ArgumentNullException(nameof(elementType));
    }

    public TypeKind Kind => TypeKind.List;

    public IType ElementType { get; }
}

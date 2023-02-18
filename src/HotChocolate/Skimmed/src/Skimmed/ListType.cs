namespace HotChocolate.Skimmed;

public sealed class ListType : IType
{
    public ListType(IType elementType)
    {
        if (elementType is null)
        {
            throw new ArgumentNullException(nameof(elementType));
        }

        ElementType = elementType;
    }

    public TypeKind Kind => TypeKind.List;

    public IType ElementType { get; }
}

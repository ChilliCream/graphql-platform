#nullable enable

namespace HotChocolate.Types;

public class ListType : NonNamedType
{
    public ListType(IType elementType) : base(elementType)
    {
    }

    public override TypeKind Kind => TypeKind.List;

    public IType ElementType => InnerType;
}

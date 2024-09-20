using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

public class NonNullType : NonNamedType
{
    public NonNullType(IType type) : base(type)
    {
        if (type.Kind == TypeKind.NonNull)
        {
            throw new ArgumentException(
                TypeResources.NonNullType_TypeIsNunNullType,
                nameof(type));
        }
    }

    public override TypeKind Kind => TypeKind.NonNull;

    public IType Type => InnerType;
}

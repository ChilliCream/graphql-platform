#nullable enable
using HotChocolate.Properties;

namespace HotChocolate.Types;

public class SemanticNonNullType : NonNamedType
{
    public SemanticNonNullType(IType type) : base(type)
    {
        if (type.Kind == TypeKind.SemanticNonNull)
        {
            // TODO
            throw new ArgumentException(
                TypeResources.NonNullType_TypeIsNunNullType,
                nameof(type));
        }
    }

    public override TypeKind Kind => TypeKind.SemanticNonNull;

    public IType Type => InnerType;
}

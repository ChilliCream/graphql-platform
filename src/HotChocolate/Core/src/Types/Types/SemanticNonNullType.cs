#nullable enable
using HotChocolate.Properties;

namespace HotChocolate.Types;

public class SemanticNonNullType : NonNamedType
{
    public SemanticNonNullType(IType type) : base(type)
    {
        if (type.Kind == TypeKind.SemanticNonNull)
        {
            throw new ArgumentException(
                TypeResources.SemanticNonNullType_TypeIsSemanticNonNullType,
                nameof(type));
        }
    }

    public override TypeKind Kind => TypeKind.SemanticNonNull;

    public IType Type => InnerType;
}

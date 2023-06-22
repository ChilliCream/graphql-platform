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
            throw new ArgumentException(
                "The inner type cannot be a non-null type.",
                nameof(nullableType));
        }

        NullableType = nullableType;
    }

    public TypeKind Kind => TypeKind.NonNull;

    public IType NullableType { get; }

    public override string ToString()
        => RewriteTypeRef(this).ToString(true);
}

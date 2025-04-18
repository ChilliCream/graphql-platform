using HotChocolate.Language;
using static HotChocolate.Properties.TypesAbstractionResources;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types;

public sealed class NonNullType : IWrapperType
{
    public NonNullType(IType nullableType)
    {
        ArgumentNullException.ThrowIfNull(nullableType);

        if (nullableType.Kind is TypeKind.NonNull)
        {
            throw new ArgumentException(
                NonNullType_InnerTypeCannotBeNonNull,
                nameof(nullableType));
        }

        NullableType = nullableType;
    }

    public TypeKind Kind => TypeKind.NonNull;

    public IType NullableType { get; }

    IType IWrapperType.InnerType => NullableType;

    public override string ToString()
        => FormatTypeRef(this).ToString(true);

    public ISyntaxNode ToSyntaxNode()
        => FormatTypeRef(this);

    public bool Equals(IType? other)
        => Equals(other, TypeComparison.Reference);

    public bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is NonNullType otherNonNull &&
            NullableType.Equals(otherNonNull.NullableType, comparison);
    }
}

using HotChocolate.Language;
using static HotChocolate.Types.Mutable.Properties.SkimmedResources;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types.Mutable;

public sealed class NonNullType : INonNullType
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

using HotChocolate.Language;
using static HotChocolate.Properties.TypesAbstractionResources;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types;

public sealed class NonNullType : IWrapperType, ISyntaxNodeProvider
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

    /// <summary>
    /// Creates a <see cref="NonNullTypeNode"/> from the current <see cref="NonNullType"/>.
    /// </summary>
    /// <returns>
    /// Returns a <see cref="NonNullTypeNode"/>.
    /// </returns>
    public NonNullTypeNode ToSyntaxNode()
        => (NonNullTypeNode)FormatTypeRef(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
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

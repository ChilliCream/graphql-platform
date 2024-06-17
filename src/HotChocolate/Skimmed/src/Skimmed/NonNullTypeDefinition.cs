using HotChocolate.Types;
using static HotChocolate.Skimmed.Properties.SkimmedResources;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class NonNullTypeDefinition : ITypeDefinition
{
    public NonNullTypeDefinition(ITypeDefinition nullableType)
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

    public ITypeDefinition NullableType { get; }

    public override string ToString()
        => RewriteTypeRef(this).ToString(true);

    public bool Equals(ITypeDefinition? other)
        => Equals(other, TypeComparison.Reference);

    public bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is NonNullTypeDefinition otherNonNull &&
            NullableType.Equals(otherNonNull.NullableType, comparison);
    }
}

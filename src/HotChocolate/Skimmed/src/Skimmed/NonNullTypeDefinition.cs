using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Skimmed.Properties.SkimmedResources;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class NonNullTypeDefinition : ITypeDefinition, IReadOnlyWrapperType
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

    IReadOnlyTypeDefinition IReadOnlyWrapperType.Type => NullableType;

    public override string ToString()
        => RewriteTypeRef(this).ToString(true);

    public ISyntaxNode ToSyntaxNode()
        => RewriteTypeRef(this);

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

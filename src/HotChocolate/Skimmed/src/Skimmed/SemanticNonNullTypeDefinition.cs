using HotChocolate.Skimmed.Properties;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class SemanticNonNullTypeDefinition : ITypeDefinition
{
    public SemanticNonNullTypeDefinition(ITypeDefinition nullableType)
    {
        ArgumentNullException.ThrowIfNull(nullableType);

        if (nullableType.Kind is TypeKind.NonNull)
        {
            throw new ArgumentException(
                // TODO: Other message
                SkimmedResources.NonNullType_InnerTypeCannotBeNonNull,
                nameof(nullableType));
        }

        NullableType = nullableType;
    }

    public TypeKind Kind => TypeKind.SemanticNonNull;

    public ITypeDefinition NullableType { get; }

    public override string ToString()
        => SchemaDebugFormatter.RewriteTypeRef(this).ToString(true);

    public bool Equals(ITypeDefinition? other)
        => Equals(other, TypeComparison.Reference);

    public bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is SemanticNonNullTypeDefinition otherSemanticNonNull &&
            NullableType.Equals(otherSemanticNonNull.NullableType, comparison);
    }
}

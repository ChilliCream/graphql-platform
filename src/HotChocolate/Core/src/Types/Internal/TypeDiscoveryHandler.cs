#nullable enable
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Internal;

public abstract class TypeDiscoveryHandler
{
    public abstract bool TryInferType(
        ExtendedTypeReference typeReference,
        TypeDiscoveryInfo typeReferenceInfo,
        [NotNullWhen(true)] out ITypeReference[]? schemaTypeRefs);

    public virtual bool TryInferKind(
        ExtendedTypeReference typeReference,
        TypeDiscoveryInfo typeReferenceInfo,
        out TypeKind typeKind)
    {
        typeKind = default;
        return false;
    }
}

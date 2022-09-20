#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed class ScalarTypeDiscoveryHandler : TypeDiscoveryHandler
{
    public ScalarTypeDiscoveryHandler(ITypeInspector typeInspector)
    {
        TypeInspector = typeInspector ?? throw new ArgumentNullException(nameof(typeInspector));
    }

    private ITypeInspector TypeInspector { get; }

    public override bool TryInferType(
        ExtendedTypeReference typeReference,
        TypeDiscoveryInfo typeReferenceInfo,
        [NotNullWhen(true)] out ITypeReference[]? schemaTypeRefs)
    {
        if (Scalars.TryGetScalar(typeReference.Type.Type, out var scalarType))
        {
            var schemaTypeRef = TypeInspector.GetTypeRef(scalarType);
            schemaTypeRefs = new ITypeReference[] { schemaTypeRef };
            return true;
        }

        schemaTypeRefs = null;
        return false;
    }

    public override bool TryInferKind(
        ExtendedTypeReference typeReference,
        TypeDiscoveryInfo typeReferenceInfo,
        out TypeKind typeKind)
    {
        if (Scalars.TryGetScalar(typeReference.Type.Type, out _))
        {
            typeKind = TypeKind.Scalar;
            return true;
        }

        typeKind = default;
        return false;
    }
}

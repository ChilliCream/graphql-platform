using System.Diagnostics.CodeAnalysis;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration;

internal static class TypeDiscoveryDescriptorContextExtensions
{
    public static bool TryInferSchemaType(
        this IDescriptorContext context,
        TypeReference unresolvedTypeRef,
        [NotNullWhen(true)] out TypeReference[]? schemaTypeRefs)
    {
        var info = new TypeDiscoveryInfo(unresolvedTypeRef);

        foreach (var handler in context.GetTypeDiscoveryHandlers())
        {
            if (handler.TryInferType(unresolvedTypeRef, info, out schemaTypeRefs))
            {
                return true;
            }
        }

        schemaTypeRefs = null;
        return false;
    }
    public static bool TryInferSchemaTypeKind(
        this IDescriptorContext context,
        ExtendedTypeReference unresolvedTypeRef,
        out TypeKind kind)
    {
        var typeReferenceInfo = new TypeDiscoveryInfo(unresolvedTypeRef);

        foreach (var handler in context.GetTypeDiscoveryHandlers())
        {
            if (handler.TryInferKind(unresolvedTypeRef, typeReferenceInfo, out kind))
            {
                return true;
            }
        }

        kind = default;
        return false;
    }
}

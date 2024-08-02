#nullable enable
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal static class TypeRefHelper
{
    public static TypeReference CreateTypeRef(
        this ITypeInspector typeInspector,
        Type schemaType,
        TypeDiscoveryInfo typeInfo,
        TypeReference originalTypeRef)
        => typeInspector.GetTypeRef(
            schemaType.MakeGenericType(typeInfo.RuntimeType),
            originalTypeRef.Context,
            originalTypeRef.Scope);
}

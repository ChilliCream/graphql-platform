#nullable enable
using System;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal static class TypeRefHelper
{
    public static ITypeReference CreateTypeRef(
        this ITypeInspector typeInspector,
        Type schemaType,
        TypeDiscoveryInfo typeInfo,
        ITypeReference originalTypeRef)
        => typeInspector.GetTypeRef(
            schemaType.MakeGenericType(typeInfo.RuntimeType),
            originalTypeRef.Context,
            originalTypeRef.Scope);
}

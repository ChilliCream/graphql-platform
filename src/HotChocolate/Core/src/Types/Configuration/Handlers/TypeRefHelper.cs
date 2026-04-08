using System.Diagnostics.CodeAnalysis;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal static class TypeRefHelper
{
    [RequiresDynamicCode("Uses MakeGenericType to create a generic schema type at runtime.")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055",
        Justification = "Schema types are well-known generic types (ObjectType<>, InputObjectType<>, etc.) and their requirements are satisfied.")]
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

using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

internal sealed class MutationResultTypeDiscoveryHandler(ITypeInspector typeInspector) : TypeDiscoveryHandler
{
    public override bool TryInferType(
        TypeReference typeReference,
        TypeDiscoveryInfo typeInfo,
        [NotNullWhen(true)] out TypeReference[]? schemaTypeRefs)
    {
        var runtimeType = typeInfo.RuntimeType;

        if (runtimeType is { IsValueType: true, IsGenericType: true, } &&
            typeof(IMutationResult).IsAssignableFrom(runtimeType) &&
            typeReference is ExtendedTypeReference typeRef)
        {
            var type = GetNamedType(typeInspector.GetType(runtimeType.GenericTypeArguments[0]));
            schemaTypeRefs = new TypeReference[runtimeType.GenericTypeArguments.Length];
            schemaTypeRefs[0] = typeRef.WithType(type);

            for (var i = 1; i < runtimeType.GenericTypeArguments.Length; i++)
            {
                var errorType = runtimeType.GenericTypeArguments[i];

                type = typeInspector.GetType(
                    typeof(Exception).IsAssignableFrom(errorType)
                        ? typeof(ExceptionObjectType<>).MakeGenericType(errorType)
                        : typeof(ErrorObjectType<>).MakeGenericType(errorType));

                schemaTypeRefs[i] = typeRef.WithType(type);
            }

            return true;
        }

        schemaTypeRefs = null;
        return false;
    }

    private IExtendedType GetNamedType(IExtendedType extendedType)
    {
        var typeInfo = typeInspector.CreateTypeInfo(extendedType);
        return typeInspector.GetType(typeInfo.NamedType);
    }
}

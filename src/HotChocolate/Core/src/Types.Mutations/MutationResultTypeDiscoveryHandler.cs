using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

internal sealed class MutationResultTypeDiscoveryHandler : TypeDiscoveryHandler
{
    private readonly ITypeInspector _typeInspector;

    public MutationResultTypeDiscoveryHandler(ITypeInspector typeInspector)
    {
        _typeInspector = typeInspector ?? throw new ArgumentNullException(nameof(typeInspector));
    }

    public override bool TryInferType(
        TypeReference typeReference,
        TypeDiscoveryInfo typeInfo,
        [NotNullWhen(true)] out TypeReference[]? schemaTypeRefs)
    {
        var runtimeType = typeInfo.RuntimeType;

        if (runtimeType is { IsValueType: true, IsGenericType: true } &&
            typeof(IMutationResult).IsAssignableFrom(runtimeType) &&
            typeReference is ExtendedTypeReference typeRef)
        {
            var type = _typeInspector.GetType(runtimeType.GenericTypeArguments[0]);
            schemaTypeRefs = new TypeReference[runtimeType.GenericTypeArguments.Length];
            schemaTypeRefs[0] = typeRef.WithType(type);

            for (var i = 1; i < runtimeType.GenericTypeArguments.Length; i++)
            {
                var errorType = runtimeType.GenericTypeArguments[i];

                type = _typeInspector.GetType(
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
}

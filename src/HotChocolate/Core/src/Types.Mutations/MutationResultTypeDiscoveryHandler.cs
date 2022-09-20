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
        ExtendedTypeReference typeReference,
        TypeDiscoveryInfo typeReferenceInfo,
        [NotNullWhen(true)] out ITypeReference[]? schemaTypeRefs)
    {
        var runtimeType = typeReference.Type.Type;

        if (runtimeType is { IsValueType: true, IsGenericType: true } &&
            typeReference.Type.Definition is {  } typeDef)
        {
            if (typeDef == typeof(MutationResult<>) ||
                typeDef == typeof(MutationResult<,>))
            {
                var type = _typeInspector.GetType(runtimeType.GenericTypeArguments[0]);
                schemaTypeRefs = new ITypeReference[runtimeType.GenericTypeArguments.Length];
                schemaTypeRefs[0] = typeReference.WithType(type);

                for(var i = 1; i < runtimeType.GenericTypeArguments.Length; i++)
                {
                    var errorType = runtimeType.GenericTypeArguments[i];

                    type = _typeInspector.GetType(typeof(Exception).IsAssignableFrom(errorType)
                        ? typeof(ExceptionObjectType<>).MakeGenericType(errorType)
                        : typeof(ErrorObjectType<>).MakeGenericType(errorType));

                    schemaTypeRefs[i] = typeReference.WithType(type);
                };

                return true;
            }
        }

        schemaTypeRefs = null;
        return false;
    }
}

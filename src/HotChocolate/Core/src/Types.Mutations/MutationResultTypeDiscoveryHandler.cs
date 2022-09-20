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
                schemaTypeRefs = new ITypeReference[]
                {
                    typeReference.WithType(
                        _typeInspector.GetType(
                            runtimeType.GenericTypeArguments[0]))
                };
                return true;
            }
        }

        schemaTypeRefs = null;
        return false;
    }
}

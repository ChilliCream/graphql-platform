using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed class SyntaxTypeReferenceHandler(ITypeInspector typeInspector) : ITypeRegistrarHandler
{
    private readonly HashSet<string> _handled = [];
    private readonly ITypeInspector _typeInspector = typeInspector ??
        throw new ArgumentNullException(nameof(typeInspector));

    public TypeReferenceKind Kind => TypeReferenceKind.Syntax;

    public void Handle(ITypeRegistrar typeRegistrar, TypeReference typeReference)
    {
        var typeRef = (SyntaxTypeReference)typeReference;

        if (_handled.Add(typeRef.Name) &&
            Scalars.TryGetScalar(typeRef.Name, out var scalarType))
        {
            var namedTypeReference = _typeInspector.GetTypeRef(scalarType);

            if (!typeRegistrar.IsResolved(namedTypeReference))
            {
                typeRegistrar.Register(
                    typeRegistrar.CreateInstance(namedTypeReference.Type.Type),
                    typeRef.Scope);
            }
        }
    }
}

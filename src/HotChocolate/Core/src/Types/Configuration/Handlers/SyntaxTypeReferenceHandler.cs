using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed class SyntaxTypeReferenceHandler : ITypeRegistrarHandler
{
    private readonly HashSet<string> _handled = [];
    private readonly ITypeInspector _typeInspector;
    private readonly Dictionary<string, Type> _scalarTypes;

    public SyntaxTypeReferenceHandler(IDescriptorContext context)
    {
        _typeInspector = context.TypeInspector;
        _scalarTypes = context.Features.Get<TypeSystemFeature>()?.ScalarNameOverrides ?? [];
    }

    public TypeReferenceKind Kind => TypeReferenceKind.Syntax;

    public void Handle(ITypeRegistrar typeRegistrar, TypeReference typeReference)
    {
        var typeRef = (SyntaxTypeReference)typeReference;

        if (_handled.Add(typeRef.Name)
            && !typeRegistrar.Scalars.Contains(typeRef.Name))
        {
            ExtendedTypeReference? scalarTypeRef = null;

            if (_scalarTypes.TryGetValue(typeRef.Name, out var scalarType))
            {
                if (Scalars.IsSpec(typeRef.Name))
                {
                    throw new InvalidOperationException(
                        $"Type {typeRef.Name} is a spec scalar and cannot be overriden.");
                }
            }

            if (scalarType is not null
                || Scalars.TryGetScalar(typeRef.Name, out scalarType))
            {
                scalarTypeRef = _typeInspector.GetTypeRef(scalarType);
            }

            if (scalarTypeRef is not null
                && !typeRegistrar.IsResolved(scalarTypeRef))
            {
                typeRegistrar.Register(
                    typeRegistrar.CreateInstance(scalarTypeRef.Type.Type),
                    typeRef.Scope);
            }
        }
    }
}

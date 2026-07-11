using HotChocolate.Types.Descriptors;
using HotChocolate.Types;

namespace HotChocolate.Configuration;

internal sealed class SourceGeneratorTypeReferenceHandler(
    IDescriptorContext context,
    TypeRegistry typeRegistry)
    : ITypeRegistrarHandler
{
    private readonly ExtendedTypeReferenceHandler _innerHandler = new(context.TypeInspector);

    private readonly HashSet<(string Key, int TypeHash, TypeContext Context)> _handled = [];

    public TypeReferenceKind Kind => TypeReferenceKind.Factory;

    public void Handle(ITypeRegistrar typeRegistrar, TypeReference typeReference)
    {
        var typeRef = (FactoryTypeReference)typeReference;
        var marker = (typeRef.Key, typeRef.TypeDefinition.GetHashCode(), typeRef.TypeDefinition.Context);

        if (_handled.Add(marker))
        {
            typeRegistry.Register(typeRef, typeRef.TypeDefinition);
            _innerHandler.Handle(typeRegistrar, typeRef.TypeDefinition);
        }
    }
}

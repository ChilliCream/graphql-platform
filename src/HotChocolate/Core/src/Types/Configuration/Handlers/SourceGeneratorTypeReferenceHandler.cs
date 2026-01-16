using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed class SourceGeneratorTypeReferenceHandler(
    IDescriptorContext context,
    TypeRegistry typeRegistry)
    : ITypeRegistrarHandler
{
    private readonly ExtendedTypeReferenceHandler _innerHandler = new(context.TypeInspector);

    private readonly HashSet<string> _handled = [];

    public TypeReferenceKind Kind => TypeReferenceKind.Factory;

    public void Handle(ITypeRegistrar typeRegistrar, TypeReference typeReference)
    {
        var typeRef = (FactoryTypeReference)typeReference;

        if (_handled.Add(typeRef.Key))
        {
            typeRegistry.Register(typeRef, typeRef.TypeDefinition);
            _innerHandler.Handle(typeRegistrar, typeRef.TypeDefinition);
        }
    }
}

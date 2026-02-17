using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed class SyntaxFactoryTypeReferenceHandler(IDescriptorContext context)
    : ITypeRegistrarHandler
{
    private readonly HashSet<string> _handled = [];

    public TypeReferenceKind Kind => TypeReferenceKind.SyntaxWithFactory;

    public void Handle(ITypeRegistrar typeRegistrar, TypeReference typeReference)
    {
        var typeRef = (SyntaxTypeReference)typeReference;

        if (_handled.Add(typeRef.Name))
        {
            var obj = typeRef.Factory!(context);
            typeRegistrar.Register(obj, typeRef.Scope, configure: AddTypeRef);
        }

        void AddTypeRef(RegisteredType registeredType)
            => registeredType.References.Add(typeRef);
    }
}

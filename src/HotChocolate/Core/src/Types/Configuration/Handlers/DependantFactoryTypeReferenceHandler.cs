using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed class DependantFactoryTypeReferenceHandler(IDescriptorContext context)
    : ITypeRegistrarHandler
{
    private readonly HashSet<DependantFactoryTypeReference> _handled = [];
    private readonly IDescriptorContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public TypeReferenceKind Kind => TypeReferenceKind.DependantFactory;

    public void Handle(ITypeRegistrar typeRegistrar, TypeReference typeReference)
    {
        var typeRef = (DependantFactoryTypeReference)typeReference;

        if (_handled.Add(typeRef))
        {
            var obj = typeRef.Factory(_context);
            typeRegistrar.Register(obj, typeRef.Scope, configure: AddTypeRef);
        }

        void AddTypeRef(RegisteredType registeredType)
            => registeredType.References.Add(typeRef);
    }
}

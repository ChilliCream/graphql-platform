using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed class FactoryTypeReferenceHandler : ITypeRegistrarHandler
{
    private readonly HashSet<string> _handled = new();
    private readonly IDescriptorContext _context;

    public FactoryTypeReferenceHandler(IDescriptorContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public TypeReferenceKind Kind => TypeReferenceKind.Factory;

    public void Handle(ITypeRegistrar typeRegistrar, TypeReference typeReference)
    {
        var typeRef = (SyntaxTypeReference)typeReference;

        if (_handled.Add(typeRef.Name))
        {
            var obj = typeRef.Factory!(_context);
            typeRegistrar.Register(obj, typeRef.Scope, configure: AddTypeRef);
        }

        void AddTypeRef(RegisteredType registeredType)
            => registeredType.References.Add(typeRef);
    }
}

using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed class SyntaxTypeReferenceHandler : ITypeRegistrarHandler
{
    private readonly HashSet<string> _handled = new();
    private readonly ITypeInspector _typeInspector;

    public SyntaxTypeReferenceHandler(ITypeInspector typeInspector)
    {
        _typeInspector = typeInspector ??
            throw new ArgumentNullException(nameof(typeInspector));
    }

    public TypeReferenceKind Kind => TypeReferenceKind.Syntax;

    public void Handle(ITypeRegistrar typeRegistrar, ITypeReference typeReference)
    {
        var typeRef = (SyntaxTypeReference)typeReference;

        if (_handled.Add(typeRef.Name) &&
            Scalars.TryGetScalar(typeRef.Name, out Type? scalarType))
        {
            ExtendedTypeReference namedTypeReference = _typeInspector.GetTypeRef(scalarType);

            if (!typeRegistrar.IsResolved(namedTypeReference))
            {
                typeRegistrar.Register(
                    typeRegistrar.CreateInstance(namedTypeReference.Type.Type),
                    typeRef.Scope);
            }
        }
    }
}

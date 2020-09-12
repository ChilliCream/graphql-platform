using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using System;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class SyntaxTypeReferenceHandler
        : ITypeRegistrarHandler
    {
        private readonly ITypeInspector _typeInspector;

        public SyntaxTypeReferenceHandler(ITypeInspector typeInspector)
        {
            _typeInspector = typeInspector;
        }

        public void Register(
            ITypeRegistrar typeRegistrar,
            IEnumerable<ITypeReference> typeReferences)
        {
            foreach (SyntaxTypeReference typeReference in
                typeReferences.OfType<SyntaxTypeReference>())
            {
                if (Scalars.TryGetScalar(
                    typeReference.Type.NamedType().Name.Value,
                    out Type? scalarType))
                {
                    ExtendedTypeReference namedTypeReference =
                        _typeInspector.GetTypeRef(scalarType);

                    if (!typeRegistrar.IsResolved(namedTypeReference))
                    {
                        typeRegistrar.Register(
                            typeRegistrar.CreateInstance(namedTypeReference.Type.Type),
                            typeReference.Scope);
                    }
                }
            }
        }
    }
}

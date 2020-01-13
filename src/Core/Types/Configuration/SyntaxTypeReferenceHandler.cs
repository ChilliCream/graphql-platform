using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class SyntaxTypeReferenceHandler
        : ITypeRegistrarHandler
    {
        public void Register(
            ITypeRegistrar typeRegistrar,
            IEnumerable<ITypeReference> typeReferences)
        {
            foreach (ISyntaxTypeReference typeReference in
                typeReferences.OfType<ISyntaxTypeReference>())
            {
                string typeName = typeReference.Type.NamedType().Name.Value;

                if (!typeRegistrar.IsResolved(typeName)
                    && Scalars.TryGetScalar(typeName, out IClrTypeReference namedTypeReference))
                {
                    if (!typeRegistrar.IsResolved(namedTypeReference))
                    {
                        typeRegistrar.Register(
                            typeRegistrar.CreateInstance(namedTypeReference.Type));
                    }
                }
            }
        }
    }
}

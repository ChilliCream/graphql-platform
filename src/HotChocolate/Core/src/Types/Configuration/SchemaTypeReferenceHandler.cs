using System.Linq;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class SchemaTypeReferenceHandler
        : ITypeRegistrarHandler
    {
        public void Register(
            ITypeRegistrar typeRegistrar,
            IEnumerable<ITypeReference> typeReferences)
        {
            foreach (SchemaTypeReference typeReference in
                typeReferences.OfType<SchemaTypeReference>())
            {
                if (!typeRegistrar.IsResolved(typeReference))
                {
                    typeRegistrar.Register(
                        (TypeSystemObjectBase)typeReference.Type,
                        typeReference.Scope);
                }
            }
        }
    }
}

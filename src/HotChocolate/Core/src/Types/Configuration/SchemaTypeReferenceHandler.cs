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
                    ITypeSystemMember tsm = typeReference.Type;

                    // if it is a type object we will make sure it is unwrapped.
                    if (typeReference.Type is IType type)
                    {
                        tsm = type.NamedType();
                    }

                    if (tsm is TypeSystemObjectBase tso)
                    {
                        typeRegistrar.Register(tso, typeReference.Scope);
                    }
                }
            }
        }
    }
}

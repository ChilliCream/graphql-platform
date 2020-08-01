using System.Linq;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using System;

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
                SchemaTypeReference scopedReference = typeReference;
                if (typeReference.Scope is { } && IsScalar(typeReference.Type.GetType()))
                {
                    scopedReference = scopedReference.WithScope(null);
                }
                if (!typeRegistrar.IsResolved(scopedReference))
                {
                    typeRegistrar.Register(
                        (TypeSystemObjectBase)scopedReference.Type,
                        scopedReference.Scope);
                }
            }
        }

        private static bool IsScalar(Type type) =>
            typeof(ScalarType).IsAssignableFrom(type);
    }
}

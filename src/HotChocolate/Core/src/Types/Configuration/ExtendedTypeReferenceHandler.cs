using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using ExtendedType = HotChocolate.Internal.ExtendedType;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class ExtendedTypeReferenceHandler
        : ITypeRegistrarHandler
    {
        public void Register(
            ITypeRegistrar typeRegistrar,
            IEnumerable<ITypeReference> typeReferences)
        {
            foreach (ClrTypeReference typeReference in typeReferences.OfType<ClrTypeReference>())
            {
                if (!BaseTypes.IsNonGenericBaseType(typeReference.Type.Type)
                    && TypeInfo.TryCreate(typeReference.Type, out TypeInfo? typeInfo))
                {
                    Type type = typeInfo.NamedType;

                    if (IsTypeSystemObject(type))
                    {
                        ClrTypeReference namedTypeReference = 
                            typeReference.With(ExtendedType.FromType(type));

                        if (!typeRegistrar.IsResolved(namedTypeReference))
                        {
                            typeRegistrar.Register(
                                typeRegistrar.CreateInstance(type),
                                typeReference.Scope,
                                BaseTypes.IsGenericBaseType(type));
                        }
                    }
                    else
                    {
                        TryMapToExistingRegistration(
                            typeRegistrar,
                            typeInfo,
                            typeReference.Context,
                            typeReference.Scope);
                    }
                }
            }
        }

        private void TryMapToExistingRegistration(
            ITypeRegistrar typeRegistrar,
            TypeInfo typeInfo,
            TypeContext context,
            string? scope)
        {
            ClrTypeReference? normalizedTypeRef = null;
            bool resolved = false;

            for (int i = 0; i < typeInfo.Components.Count; i++)
            {
                normalizedTypeRef = TypeReference.Create(
                    typeInfo.Components[i].Type,
                    context,
                    scope: scope);

                if (typeRegistrar.IsResolved(normalizedTypeRef))
                {
                    resolved = true;
                    break;
                }
            }

            if (!resolved && normalizedTypeRef is { })
            {
                typeRegistrar.MarkUnresolved(normalizedTypeRef);
            }
        }

        private static bool IsTypeSystemObject(Type type) =>
            typeof(TypeSystemObjectBase).IsAssignableFrom(type);
    }
}

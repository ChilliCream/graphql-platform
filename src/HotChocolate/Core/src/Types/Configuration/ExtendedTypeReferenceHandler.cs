using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using ExtendedType = HotChocolate.Internal.ExtendedType;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class ExtendedTypeReferenceHandler
        : ITypeRegistrarHandler
    {
        private readonly ITypeInspector _typeInspector;

        public ExtendedTypeReferenceHandler(ITypeInspector typeInspector)
        {
            _typeInspector = typeInspector;
        }

        public void Register(
            ITypeRegistrar typeRegistrar,
            IEnumerable<ITypeReference> typeReferences)
        {
            foreach (ClrTypeReference typeReference in typeReferences.OfType<ClrTypeReference>())
            {
                if (_typeInspector.TryCreateTypeInfo(typeReference.Type, out ITypeInfo? typeInfo) &&
                    !ExtendedType.Tools.IsNonGenericBaseType(typeInfo.NamedType))
                {
                    Type namedType = typeInfo.NamedType;
                    if (IsTypeSystemObject(namedType))
                    {
                        IExtendedType extendedType = _typeInspector.GetType(namedType);
                        ClrTypeReference namedTypeReference = typeReference.With(extendedType);

                        if (!typeRegistrar.IsResolved(namedTypeReference))
                        {
                            typeRegistrar.Register(
                                typeRegistrar.CreateInstance(namedType),
                                typeReference.Scope,
                                extendedType.IsSchemaType);
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
            ITypeInfo typeInfo,
            TypeContext context,
            string? scope)
        {
            ClrTypeReference? normalizedTypeRef = null;
            var resolved = false;

            for (var i = 0; i < typeInfo.Components.Count; i++)
            {
                normalizedTypeRef = TypeReference.Create(
                    typeInfo.Components[i].Type,
                    context,
                    scope);

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

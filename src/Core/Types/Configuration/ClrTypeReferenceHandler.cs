using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class ClrTypeReferenceHandler
        : ITypeRegistrarHandler
    {
        private readonly TypeInspector _typeInspector = TypeInspector.Default;
        public void Register(
            ITypeRegistrar typeRegistrar,
            IEnumerable<ITypeReference> typeReferences)

        {
            foreach (IClrTypeReference typeReference in typeReferences.OfType<IClrTypeReference>())
            {
                if (!BaseTypes.IsNonGenericBaseType(typeReference.Type)
                    && _typeInspector.TryCreate(typeReference.Type, out TypeInfo typeInfo))
                {
                    Type type = typeInfo.ClrType;

                    if (IsTypeSystemObject(type))
                    {
                        var namedTypeReference = new ClrTypeReference(
                            type,
                            SchemaTypeReference.InferTypeContext(type));

                        if (!typeRegistrar.IsResolved(namedTypeReference))
                        {
                            typeRegistrar.Register(
                                typeRegistrar.CreateInstance(type),
                                BaseTypes.IsGenericBaseType(type));
                        }
                    }
                    else
                    {
                        TryMapToExistingRegistration(
                            typeRegistrar,
                            typeInfo,
                            typeReference.Context);
                    }
                }
            }
        }

        private void TryMapToExistingRegistration(
            ITypeRegistrar typeRegistrar,
            TypeInfo typeInfo,
            TypeContext context)
        {
            ClrTypeReference? normalizedTypeRef = null;
            bool resolved = false;

            for (int i = 0; i < typeInfo.Components.Count; i++)
            {
                normalizedTypeRef = new ClrTypeReference(
                    typeInfo.Components[i],
                    context);

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

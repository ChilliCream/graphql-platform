using System;
using System.Collections.Generic;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class TypeRegistry2
        : ITypeRegistry2
    {
        public void RegisterType(INamedType namedType,
            ITypeBinding typeBinding = null)
        {
            if (namedType == null)
            {
                throw new ArgumentNullException(nameof(namedType));
            }

            TryUpdateNamedType(namedType);
            UpdateTypeBinding(namedType.Name, typeBinding);
        }

        public void RegisterType(TypeReference typeReference)
        {
            if (typeReference == null)
            {
                throw new ArgumentNullException(nameof(typeReference));
            }

            if (typeReference.IsNativeTypeReference())
            {
                RegisterNativeType(typeReference.NativeType);
            }
        }

        private void RegisterNativeType(Type type)
        {
            if (_typeInspector.TryCreate(type, out TypeInfo typeInfo))
            {
                if (typeof(INamedType).IsAssignableFrom(typeInfo.NativeNamedType))
                {
                    TryUpdateNamedType(typeInfo.NativeNamedType);
                }
                else
                {
                    _nativeTypes.Add(type, null);
                }
            }
        }

        private void TryUpdateNamedType(Type type)
        {
            lock (_sync)
            {
                INamedType namedType = (INamedType)_serviceProvider
                    .GetService(type);
                INamedType namedTypeRef = namedType;

                if (!_namedTypes.TryGetValue(namedType.Name, out namedTypeRef))
                {
                    namedTypeRef = namedType;
                    _namedTypes[namedTypeRef.Name] = namedTypeRef;
                }

                if (!_dotnetTypeToSchemaType.ContainsKey(type))
                {
                    _dotnetTypeToSchemaType[type] = namedTypeRef;
                }
            }
        }

        private void TryUpdateNamedType(INamedType namedType)
        {
            lock (_sync)
            {
                INamedType namedTypeRef = namedType;

                if (!_namedTypes.TryGetValue(namedType.Name, out namedTypeRef))
                {
                    namedTypeRef = namedType;
                    _namedTypes[namedTypeRef.Name] = namedTypeRef;
                }

                Type type = namedTypeRef.GetType();
                if (!BaseTypes.IsNonGenericBaseType(type)
                    && !_dotnetTypeToSchemaType.ContainsKey(type))
                {
                    _dotnetTypeToSchemaType[type] = namedTypeRef;
                }
            }
        }

        private void UpdateTypeBinding(string typeName, ITypeBinding typeBinding)
        {
            if (typeBinding != null)
            {
                lock (_sync)
                {
                    _typeBindings[typeName] = typeBinding;
                }
            }
        }
    }
}

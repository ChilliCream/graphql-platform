using System;
using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class TypeRegistry
        : ITypeRegistry
    {
        public void RegisterType(INamedType namedType,
            ITypeBinding typeBinding = null)
        {
            if (namedType == null)
            {
                throw new ArgumentNullException(nameof(namedType));
            }

            if (!_sealed)
            {
                TryUpdateNamedType(namedType);
                UpdateTypeBinding(namedType.Name, typeBinding);
            }
        }

        public void RegisterType(TypeReference typeReference)
        {
            if (typeReference == null)
            {
                throw new ArgumentNullException(nameof(typeReference));
            }

            if (!_sealed
                && typeReference.IsClrTypeReference()
                && !BaseTypes.IsNonGenericBaseType(typeReference.ClrType))
            {
                RegisterNativeType(typeReference.ClrType);
            }
        }

        private void RegisterNativeType(Type type)
        {
            if (_typeInspector.TryCreate(type, out TypeInfo typeInfo)
                && typeof(INamedType).IsAssignableFrom(
                    typeInfo.NativeNamedType))
            {
                TryUpdateNamedType(typeInfo.NativeNamedType);
            }
        }

        private void TryUpdateNamedType(Type type)
        {
            TryUpdateNamedType(
                (INamedType)_serviceFactory.CreateInstance(type));
        }

        private void TryUpdateNamedType(INamedType namedType)
        {
            INamedType namedTypeRef = namedType;

            if (!_namedTypes.TryGetValue(namedType.Name, out namedTypeRef))
            {
                namedTypeRef = namedType;
                _namedTypes[namedTypeRef.Name] = namedTypeRef;
            }

            Type type = namedTypeRef.GetType();
            if (!_dotnetTypeToSchemaType.ContainsKey(type)
                && !BaseTypes.IsNonGenericBaseType(type))
            {
                _dotnetTypeToSchemaType[type] = namedTypeRef.Name;
            }

            if (namedTypeRef is IInputType inputType
                && inputType.ClrType != null)
            {
                AddNativeTypeBinding(inputType.ClrType, namedType.Name);
            }
        }

        private void UpdateTypeBinding(string typeName, ITypeBinding typeBinding)
        {
            if (typeBinding != null)
            {
                _typeBindings[typeName] = typeBinding;
                AddNativeTypeBinding(typeBinding.Type, typeName);
            }
        }
    }
}

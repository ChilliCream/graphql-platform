using System;
using HotChocolate.Utilities;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class TypeRegistry
        : ITypeRegistry
    {
        public void RegisterType(INamedType namedType) =>
            RegisterType(namedType, null);

        public void RegisterType(INamedType namedType, ITypeBinding typeBinding)
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
                RegisterNativeType(
                    typeReference.ClrType,
                    typeReference.Context);
            }
        }

        private void RegisterNativeType(Type type, TypeContext context)
        {
            if (_typeInspector.TryCreate(type, out TypeInfo typeInfo))
            {
                if (typeof(INamedType).IsAssignableFrom(typeInfo.NamedType))
                {
                    TryUpdateNamedType(typeInfo.NamedType);
                }
                else if (!_clrTypes.ContainsKey(typeInfo.NamedType))
                {
                    _unresolvedTypes.Add(
                        new TypeReference(typeInfo.NamedType, context));
                }
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
            if (!_clrTypeToSchemaType.ContainsKey(type)
                && !BaseTypes.IsNonGenericBaseType(type))
            {
                _clrTypeToSchemaType[type] = namedTypeRef.Name;
            }

            if (namedTypeRef is IInputType inputType
                && inputType.ClrType != null)
            {
                AddNativeTypeBinding(inputType.ClrType, namedType.Name);
            }
        }

        private void UpdateTypeBinding(NameString typeName, ITypeBinding typeBinding)
        {
            if (typeBinding != null)
            {
                _typeBindings[typeName] = typeBinding;
                AddNativeTypeBinding(typeBinding.Type, typeName);
            }
        }
    }
}

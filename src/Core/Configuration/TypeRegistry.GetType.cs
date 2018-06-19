using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class TypeRegistry2
        : ITypeRegistry2
    {
        public T GetType<T>(string typeName)
            where T : IType
        {
            if (TryGetType(typeName, out T type))
            {
                return type;
            }
            return default;
        }

        public T GetType<T>(TypeReference typeReference)
            where T : IType
        {
            if (TryGetType(typeReference, out T type))
            {
                return type;
            }
            return default;
        }

        public bool TryGetType<T>(string typeName, out T type)
            where T : IType
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException(
                    "The type name mustn't be null or empty.",
                    nameof(typeName));
            }

            if (_namedTypes.TryGetValue(typeName, out INamedType namedType)
                && namedType is T t)
            {
                type = t;
                return true;
            }

            type = default;
            return false;
        }

        public bool TryGetType<T>(TypeReference typeReference, out T type)
            where T : IType
        {
            if (typeReference == null)
            {
                throw new ArgumentNullException(nameof(typeReference));
            }

            if (typeReference.IsNativeTypeReference())
            {
                return TryGetTypeFromNativeType(
                    typeReference.NativeType, out type);
            }
            else
            {
                return TryGetTypeFromAst(typeReference.Type, out type);
            }
        }

        private bool TryGetTypeFromNativeType<T>(Type nativeType, out T type)
        {
            if (_typeInspector.TryCreate(nativeType, out TypeInfo typeInfo))
            {
                if (_dotnetTypeToSchemaType.TryGetValue(
                    typeInfo.NativeNamedType, out INamedType namedType)
                    || (_nativeTypes.TryGetValue(nativeType, out namedType)
                        && namedType != null))
                {
                    IType internalType = typeInfo.TypeFactory(namedType);
                    if (internalType is T t)
                    {
                        type = t;
                        return true;
                    }
                }
            }

            type = default;
            return false;
        }

        private bool TryGetTypeFromAst<T>(ITypeNode typeNode, out T type)
            where T : IType
        {
            if (TryGetTypeFromAst(typeNode, out IType internalType)
                && internalType is T t)
            {
                type = t;
                return true;
            }

            type = default;
            return false;
        }

        private bool TryGetTypeFromAst(ITypeNode typeNode, out IType type)
        {
            if (typeNode.Kind == NodeKind.NonNullType
                && TryGetTypeFromAst(((NonNullTypeNode)typeNode).Type, out type))
            {
                type = new NonNullType(type);
                return true;
            }

            if (typeNode.Kind == NodeKind.ListType
                && TryGetTypeFromAst(((ListTypeNode)typeNode).Type, out type))
            {
                type = new ListType(type);
                return true;
            }

            if (typeNode.Kind == NodeKind.NamedType
                && TryGetType<INamedType>(((NamedTypeNode)typeNode).Name.Value,
                    out INamedType namedType))
            {
                type = namedType;
                return true;
            }

            type = default;
            return false;
        }

        public IEnumerable<INamedType> GetTypes()
        {
            return _namedTypes.Values;
        }
    }
}

using System;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal sealed class TypeReference
    {
        public TypeReference(ITypeNode type)
        {
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
        }

        public TypeReference(Type nativeType)
        {
            NativeType = nativeType
                ?? throw new ArgumentNullException(nameof(nativeType));
        }

        public Type NativeType { get; }
        public ITypeNode Type { get; }
    }

    internal static class TypeReferenceExtensions
    {
        public static bool IsNativeTypeReference(
            this TypeReference typeReference)
        {
            return typeReference.NativeType != null;
        }

        public static bool IsTypeMoreSpecific(
            this TypeReference typeReference, Type type)
        {
            if (typeReference == null
                || BaseTypes.IsSchemaType(type))
            {
                return true;
            }

            if (typeReference.IsNativeTypeReference()
                && !BaseTypes.IsSchemaType(typeReference.NativeType))
            {
                return true;
            }

            return false;
        }

        public static bool IsTypeMoreSpecific(
           this TypeReference typeReference, ITypeNode typeNode)
        {
            return typeReference == null
                || !typeReference.IsNativeTypeReference();
        }

        public static TypeReference GetMoreSpecific(
            this TypeReference typeReference, Type type)
        {
            if (type != null && typeReference.IsTypeMoreSpecific(type))
            {
                return new TypeReference(type);
            }
            return typeReference;
        }

        public static TypeReference GetMoreSpecific(
            this TypeReference typeReference, ITypeNode typeNode)
        {
            if (typeNode != null && typeReference.IsTypeMoreSpecific(typeNode))
            {
                return new TypeReference(typeNode);
            }
            return typeReference;
        }
    }
}

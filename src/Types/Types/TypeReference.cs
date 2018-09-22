using System;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public sealed class TypeReference
    {
        public TypeReference(ITypeNode type)
        {
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
        }

        public TypeReference(Type nativeType)
        {
            ClrType = nativeType
                ?? throw new ArgumentNullException(nameof(nativeType));
        }

        public Type ClrType { get; }

        public ITypeNode Type { get; }

        public override string ToString()
        {
            if (ClrType == null)
            {
                return Type.ToString();
            }
            return ClrType.GetTypeName();
        }
    }

    internal static class TypeReferenceExtensions
    {
        public static bool IsClrTypeReference(
            this TypeReference typeReference)
        {
            return typeReference.ClrType != null;
        }

        public static bool IsTypeMoreSpecific(
            this TypeReference typeReference, Type type)
        {
            if (typeReference == null
                || BaseTypes.IsSchemaType(type))
            {
                return true;
            }

            if (typeReference.IsClrTypeReference()
                && !BaseTypes.IsSchemaType(typeReference.ClrType))
            {
                return true;
            }

            return false;
        }

        public static bool IsTypeMoreSpecific(
           this TypeReference typeReference, ITypeNode typeNode)
        {
            return typeNode != null
                && (typeReference == null
                    || !typeReference.IsClrTypeReference());
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

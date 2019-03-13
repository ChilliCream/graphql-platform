using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    internal static class DescriptorHelpers
    {
        public static ITypeReference SetMoreSpecificType<TDefinition>(
            this TDefinition definition,
            Type type,
            TypeContext context)
            where TDefinition : FieldDefinitionBase
        {
            throw new NotImplementedException();
        }

        public static ITypeReference SetMoreSpecificType<TDefinition>(
            this TDefinition definition,
            ITypeNode typeNode)
            where TDefinition : FieldDefinitionBase
        {
            throw new NotImplementedException();
        }
    }

    internal static class TypeReferenceExtensions
    {
        public static bool IsClrTypeReference(
            this TypeReference typeReference)
        {
            return typeReference.ClrType != null;
        }

        public static bool IsSchemaTypeReference(
            this TypeReference typeReference)
        {
            return typeReference.SchemaType != null;
        }

        public static bool IsTypeMoreSpecific(
            this TypeReference typeReference, Type type)
        {
            if (typeReference != null
                && typeReference.IsSchemaTypeReference())
            {
                return false;
            }

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
            if (typeReference != null
                && typeReference.IsSchemaTypeReference())
            {
                return false;
            }

            return typeNode != null
                && (typeReference == null
                    || !typeReference.IsClrTypeReference());
        }

        public static TypeReference GetMoreSpecific(
            this TypeReference typeReference,
            Type type,
            TypeContext context)
        {
            if (type != null && typeReference.IsTypeMoreSpecific(type))
            {
                return new TypeReference(type, context);
            }
            return typeReference;
        }

        public static TypeReference GetMoreSpecific(
            this TypeReference typeReference,
            ITypeNode typeNode)
        {
            if (typeNode != null && typeReference.IsTypeMoreSpecific(typeNode))
            {
                return new TypeReference(typeNode);
            }
            return typeReference;
        }
    }
}

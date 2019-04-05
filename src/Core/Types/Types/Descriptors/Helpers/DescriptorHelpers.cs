using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    internal static class DescriptorHelpers
    {
        public static TDefinition SetMoreSpecificType<TDefinition>(
            this TDefinition definition,
            Type type,
            TypeContext context)
            where TDefinition : FieldDefinitionBase
        {
            if (IsTypeMoreSpecific(definition.Type, type))
            {
                definition.Type = new ClrTypeReference(type, context);
            }
            return definition;
        }

        public static TDefinition SetMoreSpecificType<TDefinition>(
            this TDefinition definition,
            ITypeNode typeNode,
            TypeContext context)
            where TDefinition : FieldDefinitionBase
        {
            if (IsTypeMoreSpecific(definition.Type, typeNode))
            {
                definition.Type = new SyntaxTypeReference(typeNode, context);
            }
            return definition;
        }

        private static bool IsTypeMoreSpecific(
            ITypeReference typeReference,
            Type type)
        {
            if (typeReference is ISchemaTypeReference)
            {
                return false;
            }

            if (typeReference == null
                || BaseTypes.IsSchemaType(type))
            {
                return true;
            }

            if (typeReference is IClrTypeReference clr
                && !BaseTypes.IsSchemaType(clr.Type))
            {
                return true;
            }

            return false;
        }

        private static bool IsTypeMoreSpecific(
           ITypeReference typeReference,
           ITypeNode typeNode)
        {
            if (typeReference is ISchemaTypeReference)
            {
                return false;
            }

            return typeNode != null
                && (typeReference == null
                    || typeReference is ISyntaxTypeReference);
        }
    }
}

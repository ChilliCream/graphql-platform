using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    internal static class DescriptorHelpers
    {
        public static TDefinition SetMoreSpecificType<TDefinition>(
            this TDefinition definition,
            IExtendedType type,
            TypeContext context)
            where TDefinition : FieldDefinitionBase
        {
            if (IsTypeMoreSpecific(definition.Type, type))
            {
                definition.Type = TypeReference.Create(type, context);
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
                definition.Type = TypeReference.Create(typeNode, context);
            }
            return definition;
        }

        private static bool IsTypeMoreSpecific(
            ITypeReference typeReference,
            IExtendedType type)
        {
            if (typeReference is SchemaTypeReference)
            {
                return false;
            }

            if (typeReference is null || type.IsSchemaType)
            {
                return true;
            }

            return typeReference is ExtendedTypeReference clr && 
                !clr.Type.IsSchemaType;
        }

        private static bool IsTypeMoreSpecific(
           ITypeReference typeReference,
           ITypeNode typeNode)
        {
            if (typeReference is SchemaTypeReference)
            {
                return false;
            }

            return typeNode != null
                && (typeReference is null
                    || typeReference is SyntaxTypeReference);
        }
    }
}

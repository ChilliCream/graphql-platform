using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    internal static class DescriptorHelpers
    {
        internal static T ExecuteFactory<T>(
            Func<T> descriptionFactory)
        {
            if (descriptionFactory == null)
            {
                throw new ArgumentNullException(nameof(descriptionFactory));
            }

            return descriptionFactory();
        }

        internal static void RewriteClrType(
            this FieldDefinitionBase fieldDefinition,
            Func<Type, TypeReference> createContext)
        {
            if (fieldDefinition.IsTypeNullable.HasValue
                    && fieldDefinition.Type.IsClrTypeReference())
            {
                fieldDefinition.Type = createContext(
                    DotNetTypeInfoFactory.Rewrite(
                        fieldDefinition.Type.ClrType,
                        !fieldDefinition.IsTypeNullable.Value,
                        !fieldDefinition.IsElementTypeNullable.Value));
            }
        }

        public static ITypeReference SetMoreSpecificType<TDescription>(
            this TDescription description,
            Type type,
            TypeContext context)
            where TDescription : FieldDefinitionBase
        {
            throw new NotImplementedException();
        }

        public static ITypeReference SetMoreSpecificType<TDescription>(
            this TDescription description,
            ITypeNode typeNode)
            where TDescription : FieldDefinitionBase
        {
            throw new NotImplementedException();
        }
    }
}

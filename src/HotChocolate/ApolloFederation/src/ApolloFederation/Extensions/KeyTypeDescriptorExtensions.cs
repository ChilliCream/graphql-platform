using System;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.ApolloFederation.Extensions
{
    public static class KeyInterfaceFieldDescriptorExtensions
    {
        public static IObjectTypeDescriptor Key(
            this IObjectTypeDescriptor descriptor,
            string fieldSet)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(
                TypeNames.Key,
                new ArgumentNode(
                    "fields",
                    new FieldSetType().ParseResult(fieldSet)
                )
            );
        }

        public static IInterfaceTypeDescriptor Key(
            this IInterfaceTypeDescriptor descriptor,
            string fieldSet)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(
                TypeNames.Key,
                new ArgumentNode(
                    "fields",
                    new FieldSetType().ParseResult(fieldSet)
                )
            );
        }

        public static void Key(
            this ObjectTypeDefinition objectTypeDefinition,
            string fieldSet)
        {
            if (objectTypeDefinition is null)
            {
                throw new ArgumentNullException(nameof(objectTypeDefinition));
            }

            var directiveNode = new DirectiveNode(
                TypeNames.Key,
                new ArgumentNode(
                    "fields",
                    fieldSet
                )
            );

            objectTypeDefinition.Directives.Add(
                new DirectiveDefinition(
                    directiveNode,
                    new SchemaTypeReference(new KeyDirectiveType())
                )
            );
        }
    }
}

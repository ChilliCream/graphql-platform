using System;
using HotChocolate.ApolloFederation;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Types
{
    public static class ApolloFederationFieldDescriptorExtensions
    {
        /// <summary>
        /// Adds the @external directive which is used to mark a field as owned by another service.
        /// This allows service A to use fields from service B while also knowing at runtime
        /// the types of that field.
        ///
        /// <example>
        /// # extended from the Users service
        /// extend type User @key(fields: "email") {
        ///   email: String @external
        ///   reviews: [Review]
        /// }
        /// </example>
        /// </summary>
        /// <param name="descriptor">
        /// The object field descriptor on which this directive shall be annotated.
        /// </param>
        /// <returns>
        /// Returns the object field descriptor.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="descriptor"/> is <c>null</c>.
        /// </exception>
        public static IObjectFieldDescriptor External(
            this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new ExternalDirectiveType());
        }

        public static IObjectTypeDescriptor Key(
            this IObjectTypeDescriptor descriptor,
            string fieldSet)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(
                WellKnownTypeNames.Key,
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
                WellKnownTypeNames.Key,
                new ArgumentNode(
                    "fields",
                    new FieldSetType().ParseResult(fieldSet)
                )
            );
        }

        public static void Key(
            this ObjectTypeDefinition objectTypeDefinition,
            string fieldSet,
            ITypeInspector typeInspector)
        {
            if (objectTypeDefinition is null)
            {
                throw new ArgumentNullException(nameof(objectTypeDefinition));
            }

            var directiveNode = new DirectiveNode(
                WellKnownTypeNames.Key,
                new ArgumentNode(
                    "fields",
                    fieldSet
                )
            );

            objectTypeDefinition.Directives.Add(
                new DirectiveDefinition(
                    directiveNode
                )
            );
        }
    }
}

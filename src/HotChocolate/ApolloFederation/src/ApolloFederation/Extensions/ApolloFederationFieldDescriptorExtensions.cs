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
                    WellKnownArgumentNames.Fields,
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
                    WellKnownArgumentNames.Fields,
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
                    WellKnownArgumentNames.Fields,
                    fieldSet
                )
            );

            objectTypeDefinition.Directives.Add(
                new DirectiveDefinition(
                    directiveNode
                )
            );
        }

        /// <summary>
        /// The @requires directive is used to annotate the required input fieldset
        /// from a base type for a resolver. It is used to develop a query plan
        /// where the required fields may not be needed by the client, but the
        /// service may need additional information from other services.
        ///
        /// <example>
        /// # extended from the Users service
        /// extend type User @key(fields: "id") {
        ///   id: ID! @external
        ///   email: String @external
        ///   reviews: [Review] @requires(fields: "email")
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
        public static IObjectFieldDescriptor Requires(
            this IObjectFieldDescriptor descriptor,
            string fieldSet)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(
                WellKnownTypeNames.Requires,
                new ArgumentNode(
                    WellKnownArgumentNames.Fields,
                    new FieldSetType().ParseResult(fieldSet)
                )
            );
        }


        /// <summary>
        /// The @provides directive is used to annotate the expected returned
        /// fieldset from a field on a base type that is guaranteed to be
        /// selectable by the gateway.
        ///
        /// <example>
        /// # extended from the Users service
        /// type Review @key(fields: "id") {
        ///     product: Product @provides(fields: "name")
        /// }
        ///
        /// extend type Product @key(fields: "upc") {
        ///     upc: String @external
        ///     name: String @external
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
        public static IObjectFieldDescriptor Provides(
            this IObjectFieldDescriptor descriptor,
            string fieldSet)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(
                WellKnownTypeNames.Provides,
                new ArgumentNode(
                    WellKnownArgumentNames.Fields,
                    new FieldSetType().ParseResult(fieldSet)
                )
            );
        }
    }
}

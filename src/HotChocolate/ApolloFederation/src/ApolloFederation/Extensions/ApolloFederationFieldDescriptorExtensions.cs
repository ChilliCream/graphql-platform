using System;
using HotChocolate.ApolloFederation;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

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

        /// <summary>
        /// Adds the @key directive which is used to indicate a combination of fields that
        /// can be used to uniquely identify and fetch an object or interface.
        /// <example>
        /// type Product @key(fields: "upc") {
        ///   upc: UPC!
        ///   name: String
        /// }
        /// </example>
        /// </summary>
        /// <param name="descriptor">
        /// The object type descriptor on which this directive shall be annotated.
        /// </param>
        /// <param name="fieldSet">
        /// The field set that describes the key.
        /// Grammatically, a field set is a selection set minus the braces.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="descriptor"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fieldSet"/> is <c>null</c> or <see cref="string.Empty"/>.
        /// </exception>
        public static IObjectTypeDescriptor Key(
            this IObjectTypeDescriptor descriptor,
            string fieldSet)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrEmpty(fieldSet))
            {
                throw new ArgumentException(
                    FieldDescriptorExtensions_Key_FieldSet_CannotBeNullOrEmpty,
                    nameof(fieldSet));
            }

            return descriptor.Directive(
                WellKnownTypeNames.Key,
                new ArgumentNode(
                    WellKnownArgumentNames.Fields,
                    new StringValueNode(fieldSet)));
        }

        /// <summary>
        /// Adds the @requires directive which is used to annotate the required
        /// input fieldset from a base type for a resolver. It is used to develop
        /// a query plan where the required fields may not be needed by the client, but the
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
        /// <param name="fieldSet">
        /// The <paramref name="fieldSet"/> describes which fields may
        /// not be needed by the client, but are required by
        /// this service as additional information from other services.
        /// Grammatically, a field set is a selection set minus the braces.
        /// </param>
        /// <returns>
        /// Returns the object field descriptor.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="descriptor"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fieldSet"/> is <c>null</c> or <see cref="string.Empty"/>.
        /// </exception>
        public static IObjectFieldDescriptor Requires(
            this IObjectFieldDescriptor descriptor,
            string fieldSet)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrEmpty(fieldSet))
            {
                throw new ArgumentException(
                    FieldDescriptorExtensions_Requires_FieldSet_CannotBeNullOrEmpty,
                    nameof(fieldSet));
            }

            return descriptor.Directive(
                WellKnownTypeNames.Requires,
                new ArgumentNode(
                    WellKnownArgumentNames.Fields,
                    new StringValueNode(fieldSet)));
        }

        /// <summary>
        /// Adds the @provides directive which is used to annotate the expected returned
        /// fieldset from a field on a base type that is guaranteed to be selectable by
        /// the gateway.
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
        /// <param name="fieldSet">
        /// The fields that are guaranteed to be selectable by the gateway.
        /// Grammatically, a field set is a selection set minus the braces.
        /// </param>
        /// <returns>
        /// Returns the object field descriptor.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="descriptor"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fieldSet"/> is <c>null</c> or <see cref="string.Empty"/>.
        /// </exception>
        public static IObjectFieldDescriptor Provides(
            this IObjectFieldDescriptor descriptor,
            string fieldSet)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrEmpty(fieldSet))
            {
                throw new ArgumentException(
                    FieldDescriptorExtensions_Provides_FieldSet_CannotBeNullOrEmpty,
                    nameof(fieldSet));
            }

            return descriptor.Directive(
                WellKnownTypeNames.Provides,
                new ArgumentNode(
                    WellKnownArgumentNames.Fields,
                    new StringValueNode(fieldSet)));
        }
    }
}

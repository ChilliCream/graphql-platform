using HotChocolate.ApolloFederation.Constants;
using HotChocolate.Language;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.ApolloFederation;
using LinkDirective = HotChocolate.ApolloFederation.Link;
using static HotChocolate.ApolloFederation.Constants.FederationContextData;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.Types;

/// <summary>
/// Provides extensions for type system descriptors.
/// </summary>
public static partial class ApolloFederationDescriptorExtensions
{
    /// <summary>
    /// Applies @composeDirective which is used to specify custom directives that should be exposed in the
    /// Supergraph schema. If not specified, by default, Supergraph schema excludes all custom directives.
    /// <example>
    /// extend schema @composeDirective(name: "@custom")
    ///   @link(url: "https://specs.apollo.dev/federation/v2.5", import: ["@composeDirective"])
    ///   @link(url: "https://myspecs.dev/custom/v1.0", import: ["@custom"])
    ///
    /// directive @custom on FIELD_DEFINITION
    ///
    /// type Query {
    ///   helloWorld: String! @custom
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// Name of the directive that should be preserved in the supergraph composition.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <c>null</c>.
    /// </exception>
    public static ISchemaTypeDescriptor ComposeDirective(this ISchemaTypeDescriptor descriptor, string name)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(name);

        return descriptor.Directive(
            WellKnownTypeNames.ComposeDirective,
            new ArgumentNode(
                WellKnownArgumentNames.Name,
                new StringValueNode(name)));
    }

    /// <summary>
    /// Applies the @interfaceObject directive which provides meta information to the router that this entity
    /// type defined within this subgraph is an interface in the supergraph. This allows you to extend functionality
    /// of an interface across the supergraph without having to implement (or even be aware of) all its implementing types.
    /// <example>
    /// type Foo @interfaceObject @key(fields: "ids") {
    ///   id: ID!
    ///   newCommonField: String
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
    public static IObjectTypeDescriptor InterfaceObject(this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Directive(WellKnownTypeNames.InterfaceObject);
    }

    

    /// <summary>
    /// Applies @link directive definitions to link the document to external schemas.
    /// External schemas are identified by their url, which optionally ends with a name and version with
    /// the following format: `{NAME}/v{MAJOR}.{MINOR}`
    ///
    /// By default, external types should be namespaced (prefixed with namespace__, e.g. key directive
    /// should be namespaced as federation__key) unless they are explicitly imported. We automatically
    /// import ALL federation directives to avoid the need for namespacing.
    ///
    /// NOTE: We currently DO NOT support full @link directive capability as it requires support for
    /// namespacing and renaming imports. This functionality may be added in the future releases.
    /// See @link specification for details.

    /// <example>
    /// extend schema @link(url: "https://specs.apollo.dev/federation/v2.5", import: ["@composeDirective"])
    ///
    /// type Query {
    ///   foo: Foo!
    /// }
    ///
    /// type Foo @key(fields: "id") {
    ///   id: ID!
    ///   name: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="url">
    /// Url of specification to be imported
    /// </param>
    /// <param name="import">
    /// Optional list of imported elements.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="url"/> is <c>null</c>.
    /// </exception>
    public static ISchemaTypeDescriptor Link(
        this ISchemaTypeDescriptor descriptor,
        string url,
        IEnumerable<string>? import)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(url);

        return descriptor.Directive(new LinkDirective(url, import?.ToList()));
    }

    

    /// <summary>
    /// Applies the @provides directive which is a router optimization hint specifying field set that
    /// can be resolved locally at the given subgraph through this particular query path. This
    /// allows you to expose only a subset of fields from the underlying entity type to be selectable
    /// from the federated schema without the need to call other subgraphs. Provided fields specified
    /// in the directive field set should correspond to a valid field on the underlying GraphQL
    /// interface/object type. @provides directive can only be used on fields returning entities.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///     id: ID!
    ///     # implies name field can be resolved locally
    ///     bar: Bar @provides(fields: "name")
    ///     # name fields are external
    ///     # so will be fetched from other subgraphs
    ///     bars: [Bar]
    /// }
    ///
    /// type Bar @key(fields: "id") {
    ///     id: ID!
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
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(fieldSet);

        return descriptor.Directive(
            WellKnownTypeNames.Provides,
            new ArgumentNode(
                WellKnownArgumentNames.Fields,
                new StringValueNode(fieldSet)));
    }



    

    

    /// <summary>
    /// Applies @extends directive which is used to represent type extensions in the schema. Federated extended types should have
    /// corresponding @key directive defined that specifies primary key required to fetch the underlying object.
    ///
    /// NOTE: Federation v2 no longer requires `@extends` directive due to the smart entity type merging. All usage of @extends
    /// directive should be removed from your Federation v2 schemas.
    /// <example>
    /// # extended from the Users service
    /// type Foo @extends @key(fields: "id") {
    ///   id: ID
    ///   description: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor<T> ExtendServiceType<T>(
        this IObjectTypeDescriptor<T> descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor
            .Extend()
            .OnBeforeCreate(d => d.ContextData[ExtendMarker] = true);

        return descriptor;
    }

    /// <summary>
    /// Applies the @interfaceObject directive which provides meta information to the router that this entity
    /// type defined within this subgraph is an interface in the supergraph. This allows you to extend functionality
    /// of an interface across the supergraph without having to implement (or even be aware of) all its implementing types.
    /// <example>
    /// type Foo @interfaceObject @key(fields: "ids") {
    ///   id: ID!
    ///   newCommonField: String
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
    public static IObjectTypeDescriptor<T> InterfaceObject<T>(this IObjectTypeDescriptor<T> descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Directive(WellKnownTypeNames.InterfaceObject);
    }

   
}

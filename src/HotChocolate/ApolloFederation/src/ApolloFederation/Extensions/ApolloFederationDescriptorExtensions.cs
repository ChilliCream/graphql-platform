using HotChocolate.ApolloFederation.Constants;
using HotChocolate.Language;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.ApolloFederation;
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



    

    

    

    

   
}

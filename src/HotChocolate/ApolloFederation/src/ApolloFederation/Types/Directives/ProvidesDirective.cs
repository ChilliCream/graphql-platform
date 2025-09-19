using HotChocolate.Language;
using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.FederationVersionUrls;
using static HotChocolate.ApolloFederation.Properties.FederationResources;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @provides(fields: _FieldSet!) on FIELD_DEFINITION
/// </code>
///
/// The @provides directive is a router optimization hint specifying field set that
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
[Package(Federation20)]
[DirectiveType(ProvidesDirective_Name, DirectiveLocation.FieldDefinition)]
[GraphQLDescription(ProvidesDirective_Description)]
public sealed class ProvidesDirective
{
    public ProvidesDirective(string fields)
    {
        ArgumentException.ThrowIfNullOrEmpty(fields);
        Fields = FieldSetType.ParseSelectionSet(fields);
    }

    public ProvidesDirective(SelectionSetNode fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Fields = fields;
    }

    [FieldSet]
    public SelectionSetNode Fields { get; }
}

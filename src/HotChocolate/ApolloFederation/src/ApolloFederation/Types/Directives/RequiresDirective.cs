using HotChocolate.Language;
using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.FederationVersionUrls;
using static HotChocolate.ApolloFederation.Properties.FederationResources;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @requires(fields: _FieldSet!) on FIELD_DEFINITION
/// </code>
///
/// The @requires directive is used to specify external (provided by other subgraphs)
/// entity fields that are needed to resolve target field. It is used to develop a query plan where
/// the required fields may not be needed by the client, but the service may need additional
/// information from other subgraphs. Required fields specified in the directive field set should
/// correspond to a valid field on the underlying GraphQL interface/object and should be instrumented
/// with @external directive.
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID!
///   # this field will be resolved from other subgraph
///   remote: String @external
///   local: String @requires(fields: "remote")
/// }
/// </example>
/// </summary>
[Package(Federation20)]
[DirectiveType(RequiresDirective_Name, DirectiveLocation.FieldDefinition)]
[GraphQLDescription(RequiresDirective_Description)]
public sealed class RequiresDirective
{
    public RequiresDirective(string fields)
    {
        ArgumentException.ThrowIfNullOrEmpty(fields);
        Fields = FieldSetType.ParseSelectionSet(fields);
    }

    public RequiresDirective(SelectionSetNode fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Fields = fields;
    }

    [FieldSet]
    public SelectionSetNode Fields { get; }
}

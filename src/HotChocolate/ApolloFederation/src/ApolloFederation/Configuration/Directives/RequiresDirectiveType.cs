using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Properties;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// <code>
/// directive @requires(fields: _FieldSet!) on FIELD_DEFINITON
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
public sealed class RequiresDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
        => descriptor
            .Name(WellKnownTypeNames.Requires)
            .Description(FederationResources.RequiresDirective_Description)
            .Location(DirectiveLocation.FieldDefinition)
            .FieldsArgumentV1();
}

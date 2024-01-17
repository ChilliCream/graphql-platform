namespace HotChocolate.ApolloFederation.Types;

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
[DirectiveType(DirectiveLocation.FieldDefinition)]
[GraphQLDescription(Descriptions.RequiresDirective_Description)]
public sealed class RequiresDirective(string fieldSet)
{
    [FieldSet]
    public string FieldSet { get; } = fieldSet;
}
using HotChocolate.Serialization;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class EmptyFederationQueryRootTests
{
    [Fact]
    public void Transform_Should_RemoveSyntheticQuery_When_MutationOnlySchemaHasNoKey()
    {
        TransformAndDescribe(
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.3") {
              query: Query
              mutation: Mutation
            }

            type Query {
              _service: _Service!
            }

            type Mutation {
              ping: Boolean
            }

            type _Service {
              sdl: String
            }

            directive @link(url: String!) repeatable on SCHEMA
            """).MatchInlineSnapshot(
            """
            Query root: <none>
            Query fields: <none>
            Query description: <none>
            Query directives: <none>
            Mutation root: Mutation
            Has Value type: False
            Has Query type: False
            Query field is lookup: False
            """);
    }

    [Fact]
    public void Transform_Should_RemoveSyntheticQuery_When_ValueOnlySchemaHasNoKey()
    {
        TransformAndDescribe(
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.3") {
              query: Query
            }

            type Query {
              _service: _Service!
            }

            type Value {
              name: String
            }

            type _Service {
              sdl: String
            }

            directive @link(url: String!) repeatable on SCHEMA
            """).MatchInlineSnapshot(
            """
            Query root: <none>
            Query fields: <none>
            Query description: <none>
            Query directives: <none>
            Mutation root: <none>
            Has Value type: True
            Has Query type: False
            Query field is lookup: False
            """);
    }

    [Fact]
    public void Transform_Should_GenerateLookupQuery_When_TransportQueryHasResolvableKey()
    {
        TransformAndDescribe(
            """"
            schema @link(url: "https://specs.apollo.dev/federation/v2.3") {
              query: Query
            }

            """Transport query"""
            type Query @audit {
              _entities(representations: [_Any!]!): [_Entity]!
              _service: _Service!
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type _Service {
              sdl: String
            }

            union _Entity = Product

            scalar _Any
            scalar FieldSet

            directive @key(fields: FieldSet!, resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @audit on OBJECT
            directive @link(url: String!) repeatable on SCHEMA
            """").MatchInlineSnapshot(
            """
            Query root: Query
            Query fields: fusion__lookup_productById
            Query description: Transport query
            Query directives: audit
            Mutation root: <none>
            Has Value type: False
            Has Query type: True
            Query field is lookup: True
            """);
    }

    [Fact]
    public void Transform_Should_RetainQuery_When_QueryHasRealField()
    {
        TransformAndDescribe(
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.3") {
              query: Query
            }

            type Query {
              ping: Boolean
              _service: _Service!
            }

            type _Service {
              sdl: String
            }

            directive @link(url: String!) repeatable on SCHEMA
            """).MatchInlineSnapshot(
            """
            Query root: Query
            Query fields: ping
            Query description: <none>
            Query directives: <none>
            Mutation root: <none>
            Has Value type: False
            Has Query type: True
            Query field is lookup: False
            """);
    }

    private static string TransformAndDescribe(string sourceSchema)
    {
        var result = FederationSchemaTransformer.Transform(sourceSchema);
        Assert.True(result.IsSuccess);

        var schema = SchemaParser.Parse(result.Value);
        return Describe(schema);
    }

    private static string Describe(MutableSchemaDefinition schema)
    {
        var queryType = schema.QueryType;
        var queryFields = queryType is null
            ? "<none>"
            : string.Join(", ", queryType.Fields.AsEnumerable().Select(field => field.Name));
        var queryDirectives = queryType is null || queryType.Directives.Count == 0
            ? "<none>"
            : string.Join(", ", queryType.Directives.AsEnumerable().Select(directive => directive.Name));
        var queryFieldIsLookup = queryType?.Fields.Count == 1
            && queryType.Fields[0].Directives.ContainsName("lookup");

        return $"""
            Query root: {queryType?.Name ?? "<none>"}
            Query fields: {queryFields}
            Query description: {queryType?.Description ?? "<none>"}
            Query directives: {queryDirectives}
            Mutation root: {schema.MutationType?.Name ?? "<none>"}
            Has Value type: {schema.Types.ContainsName("Value")}
            Has Query type: {schema.Types.ContainsName("Query")}
            Query field is lookup: {queryFieldIsLookup}
            """;
    }
}

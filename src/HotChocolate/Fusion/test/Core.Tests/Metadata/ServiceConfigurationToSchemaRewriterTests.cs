using CookieCrumble;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

public class ServiceConfigurationToSchemaRewriterTests
{
    [Fact]
    public void Remove_Configuration_Directives()
    {
        // arrange
        const string serviceDefinition = @"
            type Query {
              personById(id: ID!): Person
                @abc_variable(name: ""personId"", argument: ""id"")
                @abc_source(schema: ""a"")
                @abc_fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @abc_fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"")
            }

            type Person
              @abc_variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @abc_variable(name: ""personId"", select: ""id"" schema: ""b"" type: ""ID!"")
              @abc_fetch(schema: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @abc_fetch(schema: ""b"", select: ""node(id: $personId) { ... on Person { ... Person } }"") {

              id: ID!
                @abc_source(schema: ""a"")
                @abc_source(schema: ""b"")
              name: String!
                @abc_source(schema: ""a"")
              bio: String
                @abc_source(schema: ""b"")
            }

            schema
              @fusion(prefix: ""abc"")
              @abc_httpClient(schema: ""a"" baseAddress: ""https://a/graphql"")
              @abc_httpClient(schema: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var document = Utf8GraphQLParser.Parse(serviceDefinition);

        // act
        var context = ConfigurationDirectiveNamesContext.From(document);
        var rewriter = new ServiceConfigurationToSchemaRewriter();
        var rewritten = rewriter.Rewrite(document, context);

        // assert
        Snapshot
            .Create()
            .Add(rewritten)
            .MatchInline(
                @"type Query {
                  personById(id: ID!): Person
                }

                type Person {
                  id: ID!
                  name: String!
                  bio: String
                }

                schema {
                  query: Query
                }");
    }
}

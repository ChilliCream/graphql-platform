namespace HotChocolate.Fusion.Planning;

public sealed class ClientDirectivePlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_ForwardClientDirective_When_EveryApolloSubgraphDefinesIt()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: catalog
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            directive @marker(note: String!) on FIELD

            type Product @key(fields: "id") {
              id: ID!
              name: String
            }

            type Query {
              products: [Product]
            }
            """,
            """
            # name: pricing
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            directive @marker(note: String!) on FIELD

            type Product @key(fields: "id") {
              id: ID!
              price: Int
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products @marker(note: "root") {
                id
                name
                price @marker(note: "entity")
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }
}

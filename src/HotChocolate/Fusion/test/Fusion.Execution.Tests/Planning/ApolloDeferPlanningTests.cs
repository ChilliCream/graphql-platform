namespace HotChocolate.Fusion.Planning;

public sealed class ApolloDeferPlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_KeepNestedLookupRequirementInDeferredScope_When_DeferTraversesSubgraphs()
    {
        // arrange
        var schema = ComposeSchema(
            """
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
              products: [Product!]!
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """,
            """
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Product @key(fields: "id") {
              id: ID!
              related: Product!
            }
            """,
            """
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Product @key(fields: "id") {
              id: ID!
              name: String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                name
                ... @defer(label: "deeper") {
                  related {
                    name
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }
}

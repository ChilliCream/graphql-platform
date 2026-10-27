namespace HotChocolate.Fusion.Planning;

public sealed class ApolloExternalTransitionPlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_ResolveFieldThroughKey_When_FieldAboveIsExternalInRequiringSubgraph()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@shareable", "@external", "@requires"])

            type Query {
              e: E @shareable
            }

            type E @key(fields: "id") @shareable {
              id: ID!
              next: T @external
              r: String @requires(fields: "next { x }")
            }

            type T @key(fields: "id") @shareable {
              id: ID!
              x: String @external
              y: String
            }
            """,
            """
            # name: b
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@shareable"])

            type Query {
              e: E @shareable
            }

            type E @shareable {
              id: ID!
              next: T
            }

            type T @shareable {
              id: ID!
              x: String
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              e {
                r
                next {
                  x
                  y
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }
}

namespace HotChocolate.Fusion.Planning;

public sealed class GreedyPlanningFallbackTests : FusionTestBase
{
    [Fact]
    public void CreatePlan_Should_CompleteGreedyPass_When_CheapestRequirementCandidateDeadEnds()
    {
        // arrange
        // k is external to schema a, so the inline candidate of f dead-ends and its lookup sibling completes
        var schema = ComposeSchema(
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@external", "@requires"])

            type Query {
              root: T
            }

            type T @key(fields: "id") {
              id: ID!
              k: Int @external
              f: Int @requires(fields: "k")
            }
            """,
            KeyProvider,
            KeyProvider,
            KeyProvider,
            KeyProvider,
            KeyProvider,
            KeyProvider);

        // act
        var plan = PlanOperation(schema, "{ root { f } }");

        // assert
        MatchSnapshot(plan);
    }

    private const string KeyProvider =
        """
        extend schema
          @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

        type T @key(fields: "id") {
          id: ID!
          k: Int @shareable
        }
        """;
}

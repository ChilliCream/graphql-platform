using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

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

    [Fact]
    public void CreatePlan_Should_FallBackToNextRootCandidate_When_CheapestRootCandidateDeadEnds()
    {
        // arrange
        // schema a covers every field and ranks first, but it has no key to reach the source of k
        var schema = CreateRootDeadEndSchema();
        var options = new OperationPlannerOptions { MaxPlanImprovementNodes = 1 };

        // act
        var plan = PlanOperation(schema, "{ root { f g h } }", options);

        // assert
        // the greedy incumbent from the second root candidate starts the improvement budget, which one expansion spends
        Assert.Equal(1, plan.ExpandedNodes);
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_CountRootFallbackExpansions_When_MaxExpandedNodesIsSet()
    {
        // arrange
        var schema = CreateRootDeadEndSchema();
        var operation = ParseOperation("{ root { f g h } }");

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => CreatePlanner(schema, new OperationPlannerOptions { MaxExpandedNodes = 9 })
                .CreatePlan(
                    "guardrail-greedy-root-fallback-exceeded",
                    "hash",
                    "12345678",
                    operation,
                    TestContext.Current.CancellationToken));
        var plan = CreatePlanner(schema, new OperationPlannerOptions { MaxExpandedNodes = 10 })
            .CreatePlan(
                "guardrail-greedy-root-fallback-sufficient",
                "hash",
                "12345678",
                operation,
                TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(OperationPlannerGuardrailReason.MaxExpandedNodesExceeded, error.Reason);
        Assert.Equal(9, error.Limit);
        Assert.Equal(10, error.Observed);
        Assert.Equal(6, plan.ExpandedNodes);
    }

    private static FusionSchemaDefinition CreateRootDeadEndSchema()
        => ComposeSchema(
            """
            # name: a
            type Query {
              root: T @shareable
            }

            type T {
              g: Int @shareable
              h: Int @shareable
              f(k: Int @require(field: "k")): Int @shareable
            }
            """,
            """
            # name: b
            type Query {
              root: T @shareable
              tById(id: ID!): T @lookup @internal
            }

            type T @key(fields: "id") {
              id: ID!
              k: Int
              f: Int @shareable
            }
            """,
            """
            # name: c
            type Query {
              tById(id: ID!): T @lookup @internal
            }

            type T @key(fields: "id") {
              id: ID!
              g: Int @shareable
              h: Int @shareable
            }
            """);

    private const string KeyProvider =
        """
        extend schema
          @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

        type T @key(fields: "id") {
          id: ID!
          k: Int @shareable
        }
        """;

    private static OperationPlanner CreatePlanner(
        FusionSchemaDefinition schema,
        OperationPlannerOptions options)
    {
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var compiler = new OperationCompiler(schema, pool);

        return new OperationPlanner(schema, compiler, options);
    }

    private static OperationDefinitionNode ParseOperation([StringSyntax("graphql")] string operationText)
        => Utf8GraphQLParser.Parse(operationText).Definitions.OfType<OperationDefinitionNode>().First();
}

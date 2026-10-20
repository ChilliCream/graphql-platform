using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class LookupMergePolicyTests : FusionTestBase
{
    public static IEnumerable<object[]> SameDepthCases()
    {
        foreach (var apollo in new[] { false, true })
        {
            foreach (var grouping in new[] { false, true })
            {
                yield return [apollo, grouping, OperationMergePolicy.None, false];
                yield return [apollo, grouping, OperationMergePolicy.Conservative, true];
                yield return [apollo, grouping, OperationMergePolicy.Balanced, true];
                yield return [apollo, grouping, OperationMergePolicy.Aggressive, true];
            }
        }
    }

    [Theory]
    [MemberData(nameof(SameDepthCases))]
    public void Plan_Should_ApplyMergePolicyIndependentlyOfGrouping_When_LookupsShareDepth(
        bool apollo,
        bool grouping,
        OperationMergePolicy policy,
        bool merge)
    {
        // arrange
        var schema = CreateSchema(apollo);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products { name }
              brands { topProduct { name } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = policy,
                EnableRequestGrouping = grouping
            });

        // assert
        AssertLookupTargets(plan, grouping, merge, "$.brands.topProduct");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Plan_Should_KeepUnrelatedLookupsSeparate_When_RequestGroupingIsDisabled(bool apollo)
    {
        // arrange
        var schema = CreateSchema(apollo);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products { name }
              brands { topProduct { name } }
              other: products { price }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.Aggressive,
                EnableRequestGrouping = false
            });

        // assert
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        var targets = nodes.SelectMany(GetTargets).ToArray();
        Assert.Equal(2, nodes.Length);
        Assert.Equal(new[] { 1, 2 }, targets.Select(t => t.Length).Order());
        Assert.Equal(
            ["$.brands.topProduct", "$.other", "$.products"],
            targets.SelectMany(t => t).Order(StringComparer.Ordinal));
    }

    private static void AssertLookupTargets(
        OperationPlan plan,
        bool grouping,
        bool merge,
        string deeperTarget)
    {
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        var targets = nodes.SelectMany(GetTargets).ToArray();
        Assert.Equal(merge ? 1 : 2, targets.Length);
        Assert.Equal(merge || grouping ? 1 : 2, nodes.Length);
        Assert.Equal(
            new[] { deeperTarget, "$.products" },
            targets.SelectMany(t => t).Order(StringComparer.Ordinal));
    }

    private static IEnumerable<string[]> GetTargets(ExecutionNode node)
    {
        switch (node)
        {
            case OperationExecutionNode single:
                yield return [single.Target.ToString()];
                break;

            case ApolloOperationExecutionNode single:
                yield return [single.Target.ToString()];
                break;

            case OperationBatchExecutionNode batch:
                foreach (var operation in batch.Operations.ToArray())
                {
                    yield return GetTargets(operation);
                }
                break;

            case ApolloOperationBatchExecutionNode batch:
                foreach (var operation in batch.Operations.ToArray())
                {
                    yield return GetTargets(operation);
                }
                break;
        }
    }

    private static string[] GetTargets(OperationDefinition operation)
    {
        if (operation is SingleOperationDefinition single)
        {
            return [single.Target.ToString()];
        }

        return Assert.IsType<BatchOperationDefinition>(operation)
            .Targets.ToArray().Select(t => t.ToString()).ToArray();
    }

    private static FusionSchemaDefinition CreateSchema(bool apollo)
    {
        const string federation =
            """
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])
            """;
        var header = apollo ? federation : string.Empty;
        var featuredQuery = apollo ? string.Empty : """type Query { brandById(id: ID! @is(field: "id")): Brand @lookup @internal }""";
        var detailsQuery = apollo ? string.Empty : """type Query { productById(id: ID! @is(field: "id")): Product @lookup @internal }""";

        return ComposeSchema(
            header
            + """
            type Query {
              products: [Product!]!
              brands: [Brand!]!
            }
            type Product @key(fields: "id") { id: ID! }
            type Brand @key(fields: "id") {
              id: ID!
              topProduct: Product!
            }
            """,
            header + featuredQuery
            + """
            type Brand @key(fields: "id") {
              id: ID!
              featured: Product!
            }
            type Product @key(fields: "id") { id: ID! }
            """,
            header + detailsQuery
            + """
            type Product @key(fields: "id") {
              id: ID!
              name: String!
              price: Int!
            }
            """);
    }
}

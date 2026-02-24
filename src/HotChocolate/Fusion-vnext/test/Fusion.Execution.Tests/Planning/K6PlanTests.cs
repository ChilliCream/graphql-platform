using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class K6PlanTests : FusionTestBase
{
    [Fact]
    public void DeepNesting()
    {
        // arrange
        var schema = BenchmarkSchema();

        // act
        var plan = PlanOperation(schema, FileResource.Open("k6.graphql"));

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void DeepNesting_Grouped_Nodes_Stay_In_Same_Dependency_Depth()
    {
        // arrange
        var schema = BenchmarkSchema();

        // act
        var plan = PlanOperation(schema, FileResource.Open("k6.graphql"));
        var nodes = plan.AllNodes.ToDictionary(t => t.Id);
        var depthLookup = new Dictionary<int, int>();

        foreach (var node in nodes.Values)
        {
            GetDepth(node, nodes, depthLookup);
        }

        // assert
        foreach (var grouping in plan.AllNodes
                     .OfType<OperationExecutionNode>()
                     .Where(t => t.BatchingGroupId is not null)
                     .GroupBy(t => t.BatchingGroupId!.Value))
        {
            var depths = grouping.Select(t => depthLookup[t.Id]).Distinct().ToArray();
            Assert.Single(depths);
        }
    }

    private static FusionSchemaDefinition BenchmarkSchema()
        => ComposeSchema(
            FileResource.Open("k6-accounts.graphqls"),
            FileResource.Open("k6-inventory.graphqls"),
            FileResource.Open("k6-products.graphqls"),
            FileResource.Open("k6-reviews.graphqls"));

    private static int GetDepth(
        ExecutionNode node,
        IReadOnlyDictionary<int, ExecutionNode> nodes,
        Dictionary<int, int> depthLookup)
    {
        if (depthLookup.TryGetValue(node.Id, out var depth))
        {
            return depth;
        }

        depth = 0;

        foreach (var dependency in node.Dependencies)
        {
            if (!nodes.TryGetValue(dependency.Id, out var dependencyNode))
            {
                continue;
            }

            var dependencyDepth = GetDepth(dependencyNode, nodes, depthLookup);
            depth = Math.Max(depth, dependencyDepth + 1);
        }

        depthLookup[node.Id] = depth;
        return depth;
    }
}

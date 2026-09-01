using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Graph;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph;

public sealed class GraphSearchProjectionContextTests
{
    [Fact]
    public void Constructor_Should_PrecomputeDeepChainRepresentativesWithoutResolveWork()
    {
        // arrange
        const int count = 2_000;
        var nodes = new List<GraphNode>(count);
        var edges = new List<GraphEdge>(count - 1);

        for (var index = count - 1; index >= 0; index--)
        {
            nodes.Add(Node($"n{index:D4}"));

            if (index > 0)
            {
                edges.Add(new GraphEdge($"n{index - 1:D4}", $"n{index:D4}", GraphEdgeKind.ParentChild));
            }
        }

        var visible = new GraphModel(nodes, edges);
        var context = new GraphSearchProjectionContext(visible, new GraphModel([Node("n0000")], []));
        var visits = context.RepresentativeBuildVisitCount;

        // act
        var representatives = new[] { context.ResolveRepresentative("n0000"), context.ResolveRepresentative("n1999") };

        // assert
        Assert.Equal(["n0000", "n0000"], representatives);
        Assert.Equal(count, visits);
        Assert.Equal(visits, context.RepresentativeBuildVisitCount);
    }

    private static GraphNode Node(string id)
        => new()
        {
            Id = id,
            Title = id,
            Status = TaskStates.Open,
            Type = TaskTypes.Task,
            Priority = TaskPriorities.Medium
        };
}

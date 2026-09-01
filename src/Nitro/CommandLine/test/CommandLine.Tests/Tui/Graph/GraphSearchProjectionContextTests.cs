using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Graph;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph;

public sealed class GraphSearchProjectionContextTests
{
    [Fact]
    public void Constructor_Should_PrecomputeDeepChainRepresentativesWithoutResolveWork_When_GraphIsDeep()
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

    [Fact]
    public void Constructor_Should_KeepRepresentativesStable_When_ParentEdgesAreReversedAndCyclic()
    {
        // arrange
        var nodes = new[] { Node("a"), Node("b"), Node("c"), Node("root") with { Priority = 0 } };
        var edges = new[]
        {
            new GraphEdge("a", "b", GraphEdgeKind.ParentChild),
            new GraphEdge("b", "c", GraphEdgeKind.ParentChild),
            new GraphEdge("c", "a", GraphEdgeKind.ParentChild),
            new GraphEdge("root", "a", GraphEdgeKind.ParentChild)
        };
        var reduced = new GraphModel([Node("root")], []);

        // act
        var first = new GraphSearchProjectionContext(new GraphModel(nodes, edges), reduced);
        var second = new GraphSearchProjectionContext(new GraphModel(nodes.Reverse().ToArray(), edges.Reverse().ToArray()), reduced);

        // assert
        Assert.Equal(["root", "root", "root", "root"], nodes.Select(t => first.ResolveRepresentative(t.Id)));
        Assert.Equal(nodes.Select(t => first.ResolveRepresentative(t.Id)), nodes.Select(t => second.ResolveRepresentative(t.Id)));
        Assert.Equal(4, first.RepresentativeBuildVisitCount);
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

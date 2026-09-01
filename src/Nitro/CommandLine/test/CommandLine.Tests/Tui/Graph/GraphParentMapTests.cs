using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Graph;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph;

public sealed class GraphParentMapTests
{
    [Fact]
    public void Build_Should_ChoosePriorityParentAndDeterministicallyDetachCycles_When_InputOrderDiffers()
    {
        // arrange
        var nodes = new[]
        {
            Node("z", 0), Node("a", 4), Node("child", 2),
            Node("x", 1), Node("y", 0),
            Node("long-a", 3), Node("long-b", 2), Node("long-c", 1)
        };
        var edges = new[]
        {
            Edge("a", "child"), Edge("z", "child"), Edge("child", "child"), Edge("missing", "child"),
            Edge("x", "y"), Edge("y", "x"),
            Edge("long-a", "long-b"), Edge("long-b", "long-c"), Edge("long-c", "long-a")
        };

        // act
        var first = GraphParentMap.Build(new GraphModel(nodes, edges));
        var second = GraphParentMap.Build(new GraphModel(nodes.Reverse().ToArray(), edges.Reverse().ToArray()));

        // assert
        Assert.Equal(
            ["child:z", "long-a:long-c", "long-b:long-a", "x:y"],
            first.OrderBy(t => t.Key, StringComparer.Ordinal).Select(t => $"{t.Key}:{t.Value}"));
        Assert.Equal(
            first.OrderBy(t => t.Key, StringComparer.Ordinal).Select(t => $"{t.Key}:{t.Value}"),
            second.OrderBy(t => t.Key, StringComparer.Ordinal).Select(t => $"{t.Key}:{t.Value}"));
    }

    private static GraphNode Node(string id, int priority)
        => new()
        {
            Id = id,
            Title = id,
            Status = TaskStates.Open,
            Type = TaskTypes.Task,
            Priority = priority
        };

    private static GraphEdge Edge(string parentId, string childId)
        => new(parentId, childId, GraphEdgeKind.ParentChild);
}

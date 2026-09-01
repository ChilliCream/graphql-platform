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

    [Fact]
    public void Build_Should_CreateAnOrderStableParentChainWithoutRecursion_When_TheGraphIsDeep()
    {
        // arrange
        const int count = 2_000;
        var nodes = Enumerable.Range(0, count).Select(index => Node($"n{index:D4}", 1)).ToArray();
        var edges = Enumerable.Range(1, count - 1)
            .Select(index => Edge($"n{index - 1:D4}", $"n{index:D4}"))
            .ToArray();

        // act
        var first = GraphParentMap.Build(new GraphModel(nodes, edges));
        var second = GraphParentMap.Build(new GraphModel(nodes.Reverse().ToArray(), edges.Reverse().ToArray()));

        // assert
        Assert.Equal(count - 1, first.Count);
        Assert.Equal("n0000", first["n0001"]);
        Assert.Equal($"n{count - 2:D4}", first[$"n{count - 1:D4}"]);
        Assert.Equal(first.OrderBy(t => t.Key), second.OrderBy(t => t.Key));
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

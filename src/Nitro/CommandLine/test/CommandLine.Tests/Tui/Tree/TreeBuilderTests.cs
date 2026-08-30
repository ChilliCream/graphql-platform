using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Tree;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Tree;

public sealed class TreeBuilderTests
{
    [Fact]
    public void Build_Should_ReturnOnlyRoot_When_NoEdges()
    {
        // arrange & act
        var rows = TreeBuilder.Build("a", [], TreeEdgeMode.Blocking, TreeDirection.Up);

        // assert
        var row = Assert.Single(rows);
        Assert.Equal("a", row.TaskId);
        Assert.Equal(0, row.Depth);
        Assert.False(row.IsCycle);
    }

    [Fact]
    public void Build_Should_WalkTowardDependencies_When_DirectionIsUp()
    {
        // arrange
        // a depends on b and c
        var edges = new List<TaskDependency>
        {
            Edge("a", "b"),
            Edge("a", "c")
        };

        // act
        var rows = TreeBuilder.Build("a", edges, TreeEdgeMode.Blocking, TreeDirection.Up);

        // assert
        Assert.Equal(["a", "b", "c"], rows.Select(r => r.TaskId));
        Assert.Equal([0, 1, 1], rows.Select(r => r.Depth));
    }

    [Fact]
    public void Build_Should_WalkTowardDependents_When_DirectionIsDown()
    {
        // arrange
        // b and c depend on a
        var edges = new List<TaskDependency>
        {
            Edge("b", "a"),
            Edge("c", "a")
        };

        // act
        var rows = TreeBuilder.Build("a", edges, TreeEdgeMode.Blocking, TreeDirection.Down);

        // assert
        Assert.Equal(["a", "b", "c"], rows.Select(r => r.TaskId));
    }

    [Fact]
    public void Build_Should_OrderChildrenOrdinally()
    {
        // arrange
        var edges = new List<TaskDependency>
        {
            Edge("a", "z"),
            Edge("a", "m"),
            Edge("a", "b")
        };

        // act
        var rows = TreeBuilder.Build("a", edges, TreeEdgeMode.Blocking, TreeDirection.Up);

        // assert
        Assert.Equal(["a", "b", "m", "z"], rows.Select(r => r.TaskId));
    }

    [Fact]
    public void Build_Should_OnlyFollowParentChildEdges_When_EdgeModeIsParentChild()
    {
        // arrange
        var edges = new List<TaskDependency>
        {
            Edge("a", "b", TaskDependencyTypes.ParentChild),
            Edge("a", "c", TaskDependencyTypes.Blocks)
        };

        // act
        var rows = TreeBuilder.Build("a", edges, TreeEdgeMode.ParentChild, TreeDirection.Up);

        // assert
        Assert.Equal(["a", "b"], rows.Select(r => r.TaskId));
    }

    [Fact]
    public void Build_Should_FollowEveryBlockingType_When_EdgeModeIsBlocking()
    {
        // arrange
        var edges = new List<TaskDependency>
        {
            Edge("a", "b", TaskDependencyTypes.Blocks),
            Edge("a", "c", TaskDependencyTypes.ParentChild),
            Edge("a", "d", TaskDependencyTypes.WaitsFor),
            Edge("a", "e", TaskDependencyTypes.Related)
        };

        // act
        var rows = TreeBuilder.Build("a", edges, TreeEdgeMode.Blocking, TreeDirection.Up);

        // assert
        Assert.Equal(["a", "b", "c", "d"], rows.Select(r => r.TaskId));
    }

    [Fact]
    public void Build_Should_StopAtRequestedDepth()
    {
        // arrange
        // a -> b -> c -> d
        var edges = new List<TaskDependency>
        {
            Edge("a", "b"),
            Edge("b", "c"),
            Edge("c", "d")
        };

        // act
        var rows = TreeBuilder.Build("a", edges, TreeEdgeMode.Blocking, TreeDirection.Up, maxDepth: 1);

        // assert
        Assert.Equal(["a", "b"], rows.Select(r => r.TaskId));
    }

    [Fact]
    public void Build_Should_ClampRequestedDepth_To_HardMaxDepth()
    {
        // arrange: a chain longer than the hard cap
        var edges = new List<TaskDependency>();
        const int chainLength = TreeBuilder.HardMaxDepth + 5;

        for (var i = 0; i < chainLength; i++)
        {
            edges.Add(Edge($"t{i}", $"t{i + 1}"));
        }

        // act
        var rows = TreeBuilder.Build("t0", edges, TreeEdgeMode.Blocking, TreeDirection.Up, maxDepth: 999);

        // assert
        Assert.Equal(TreeBuilder.HardMaxDepth + 1, rows.Count);
        Assert.Equal(TreeBuilder.HardMaxDepth, rows[^1].Depth);
    }

    [Fact]
    public void Build_Should_MarkSecondOccurrence_As_Cycle_When_PathsConverge()
    {
        // arrange: diamond, a -> b -> d, a -> c -> d
        var edges = new List<TaskDependency>
        {
            Edge("a", "b"),
            Edge("a", "c"),
            Edge("b", "d"),
            Edge("c", "d")
        };

        // act
        var rows = TreeBuilder.Build("a", edges, TreeEdgeMode.Blocking, TreeDirection.Up);

        // assert
        Assert.Equal(["a", "b", "d", "c", "d"], rows.Select(r => r.TaskId));
        Assert.False(rows[2].IsCycle);
        Assert.True(rows[4].IsCycle);
    }

    [Fact]
    public void Build_Should_NotExpandBeyond_CycleRow()
    {
        // arrange: d has its own child e, but d is only reached the second
        // time as a cycle row, so e must never appear.
        var edges = new List<TaskDependency>
        {
            Edge("a", "b"),
            Edge("a", "c"),
            Edge("b", "d"),
            Edge("c", "d"),
            Edge("d", "e")
        };

        // act
        var rows = TreeBuilder.Build("a", edges, TreeEdgeMode.Blocking, TreeDirection.Up);

        // assert
        Assert.Equal(["a", "b", "d", "e", "c", "d"], rows.Select(r => r.TaskId));
        Assert.Equal(1, rows.Count(r => r.TaskId == "e"));
        Assert.True(rows[5].IsCycle);
    }

    [Fact]
    public void Build_Should_MarkAncestor_As_Cycle_When_EdgeLoopsBack()
    {
        // arrange: a -> b -> a
        var edges = new List<TaskDependency>
        {
            Edge("a", "b"),
            Edge("b", "a")
        };

        // act
        var rows = TreeBuilder.Build("a", edges, TreeEdgeMode.Blocking, TreeDirection.Up);

        // assert
        Assert.Equal(["a", "b", "a"], rows.Select(r => r.TaskId));
        Assert.False(rows[0].IsCycle);
        Assert.True(rows[2].IsCycle);
    }

    [Fact]
    public void Build_Should_SetConnectorState_ForSiblingsAndGrandchildren()
    {
        // arrange: a -> b -> d, a -> c
        var edges = new List<TaskDependency>
        {
            Edge("a", "b"),
            Edge("a", "c"),
            Edge("b", "d")
        };

        // act
        var rows = TreeBuilder.Build("a", edges, TreeEdgeMode.Blocking, TreeDirection.Up);

        // assert
        Assert.Equal(["a", "b", "d", "c"], rows.Select(r => r.TaskId));

        var b = rows[1];
        Assert.False(b.IsLastChild);
        Assert.Empty(b.AncestorIsLastChild);

        var d = rows[2];
        Assert.True(d.IsLastChild);
        Assert.Equal([false], d.AncestorIsLastChild);

        var c = rows[3];
        Assert.True(c.IsLastChild);
        Assert.Empty(c.AncestorIsLastChild);
    }

    private static TaskDependency Edge(string taskId, string dependsOnId, string type = TaskDependencyTypes.Blocks)
        => new()
        {
            TaskId = taskId,
            DependsOnId = dependsOnId,
            Type = type,
            CreatedAt = DateTimeOffset.UnixEpoch
        };
}

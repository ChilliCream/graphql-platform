namespace HotChocolate.Fusion.Text.Json;

public class CompactPathConstructionTests : FusionTestBase
{
    [Fact]
    public void CompactPath_Should_PreserveNestedIndices_When_ArraysCrossMetadataChunks()
    {
        // arrange
        var schema = ComposeSchema("type Query { matrix: [[String]] }");
        var plan = PlanOperation(schema, "{ grid: matrix }");
        var document = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var matrix = document.Data.GetProperty("grid");
        var selectionId = matrix.AssertSelection().Id;
        matrix.SetArrayValue(150);
        foreach (var item in matrix.EnumerateArray())
        {
            item.SetArrayValue(2);
        }

        // act
        var first = matrix[0][0].CompactPath;
        var last = matrix[149][1].CompactPath;

        // assert
        Assert.Equal([selectionId, ~0, ~0], first.Segments.ToArray());
        Assert.Equal([selectionId, ~149, ~1], last.Segments.ToArray());
        Assert.Equal("grid[149][1]", last.ToPath(plan.Operation).ToString());
        Assert.True(document.Data.CompactPath.IsRoot);
    }

    [Fact]
    public void CompactPath_Should_PreserveLogicalAlias_When_ReferencesShareAnObject()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var plan = PlanOperation(schema,
            "{ first: productBySlug(slug: \"1\") { name } second: productBySlug(slug: \"1\") { name } }");
        var document = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var first = document.Data.GetProperty("first");
        var second = document.Data.GetProperty("second");
        var shared = document.CreateObject(first.Cursor, plan.Operation.GetSelectionSet(first.AssertSelection()));
        document.AssignCompositeValue(first, shared);
        document.AssignCompositeValue(second, shared);
        first.SetNullMarker();

        // act
        var firstPath = first.CompactPath.ToPath(plan.Operation).ToString();
        var secondPath = second.CompactPath.ToPath(plan.Operation).ToString();
        var childPath = shared.GetProperty("name").CompactPath.ToPath(plan.Operation).ToString();

        // assert
        Assert.Equal("first", firstPath);
        Assert.Equal("second", secondPath);
        Assert.Equal("first.name", childPath);
    }

    [Fact]
    public void CompactPath_Should_PreserveDeepSegments_When_ParentChainIsBelowLimit()
    {
        // arrange
        var (document, leaf) = CreateDeepDocument(20);

        // act
        var path = leaf.CompactPath;

        // assert
        Assert.Equal(21, path.Length);
        Assert.Equal("root." + string.Concat(Enumerable.Repeat("child.", 19)) + "name",
            path.ToPath(document.Data.Operation).ToString());
    }

    [Fact]
    public void CompactPath_Should_PreserveDepthError_When_ParentChainExceedsLimit()
    {
        // arrange
        var (_, leaf) = CreateDeepDocument(21);

        // act
        var error = Record.Exception(() => leaf.CompactPath);

        // assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal("The path is to deep.", error.Message);
    }

    private static (CompositeResultDocument Document, CompositeResultElement Leaf) CreateDeepDocument(int depth)
    {
        var schema = ComposeSchema("type Query { root: Item } type Item { child: Item name: String }");
        var query = "{ root { " + string.Concat(Enumerable.Repeat("child { ", depth - 1))
            + "name " + new string('}', depth + 1);
        var plan = PlanOperation(schema, query);
        var document = new CompositeResultDocument(CommonTestExtensions.CreateArena(), plan.Operation, 0);
        var current = document.Data.GetProperty("root");
        for (var i = 0; i < depth; i++)
        {
            current.SetObjectValue(plan.Operation.GetSelectionSet(current.AssertSelection()));
            if (i + 1 < depth)
            {
                current = current.GetProperty("child");
            }
        }

        return (document, current.GetProperty("name"));
    }
}

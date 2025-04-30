using System.Text.Json;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Tests.Execution;

/// <summary>
/// Unit tests for <see cref="FetchResult"/>.
/// </summary>
public sealed class FetchResultTests
{
    [Fact]
    public void GetFromSourceData_ReturnsRoot()
    {
        const string json =
            """
            {
              "data": { "hello": "world" }
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var fetch = FetchResult.From(
            Path.Root,
            SelectionPath.Root,
            SelectionPath.Root,
            doc);

        var element = fetch.GetFromSourceData();

        Assert.Equal(JsonValueKind.Object, element.ValueKind);
        Assert.Equal("world", element.GetProperty("hello").GetString());
    }

    [Fact]
    public void GetFromSourceData_NestedField()
    {
        const string json =
            """
            {
              "data": {
                "user": { "id": 123, "name": "Luke" }
              }
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var fetch = FetchResult.From(
            Path.Root,
            SelectionPath.Root,
            SelectionPath.Parse("user.id"),
            doc);

        var id = fetch.GetFromSourceData();

        Assert.Equal(JsonValueKind.Number, id.ValueKind);
        Assert.Equal(123, id.GetInt32());
    }

    [Fact]
    public void GetFromSourceData_InlineFragment()
    {
        const string json =
            """
            {
              "data": {
                "node": {
                  "__typename": "User",
                  "id": "U1",
                  "name": "Ana"
                }
              }
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var fetch = FetchResult.From(
            Path.Root,
            SelectionPath.Root,
            SelectionPath.Parse("node.<User>.id"),
            doc);

        var id = fetch.GetFromSourceData();

        Assert.Equal(JsonValueKind.String, id.ValueKind);
        Assert.Equal("U1", id.GetString());
    }

    [Fact]
    public void GetFromSourceData_InlineFragment_Type_Mismatch()
    {
        const string json =
            """
            {
              "data": {
                "node": {
                  "__typename": "Group",
                  "id": "U1",
                  "name": "Ana"
                }
              }
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var fetch = FetchResult.From(
            Path.Root,
            SelectionPath.Root,
            SelectionPath.Parse("node.<User>.id"),
            doc);

        var id = fetch.GetFromSourceData();

        Assert.Equal(JsonValueKind.Null, id.ValueKind);
    }

    [Fact]
    public void GetFromSourceData_InlineFragment_Type_Missing()
    {
        const string json =
            """
            {
              "data": {
                "node": {
                  "id": "U1",
                  "name": "Ana"
                }
              }
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var fetch = FetchResult.From(
            Path.Root,
            SelectionPath.Root,
            SelectionPath.Parse("node.<User>.id"),
            doc);

        var id = fetch.GetFromSourceData();

        Assert.Equal(JsonValueKind.Null, id.ValueKind);
    }

    [Fact]
    public void GetFromSourceData_MissingPath_ReturnsDefault()
    {
        const string json = """{ "data": { "foo": 1 } }""";

        using var doc = JsonDocument.Parse(json);
        var fetch = FetchResult.From(
            Path.Root,
            SelectionPath.Root,
            SelectionPath.Parse("bar.baz"),
            doc);

        var result = fetch.GetFromSourceData();

        Assert.Equal(JsonValueKind.Undefined, result.ValueKind);
    }

    [Fact]
    public void CompareTo_SortsByRuntimePath()
    {
        var a = new TestBuilder("/foo");
        var b = new TestBuilder("/foo/bar");

        Assert.True(a.FetchResult.CompareTo(b.FetchResult) < 0);
        Assert.True(b.FetchResult.CompareTo(a.FetchResult) > 0);
    }

    private sealed class TestBuilder
    {
        public FetchResult FetchResult { get; }

        public TestBuilder(string runtimePath)
        {
            const string json = """{ "data": {} }""";
            using var doc = JsonDocument.Parse(json);

            FetchResult = FetchResult.From(
                Path.Parse(runtimePath),
                SelectionPath.Root,
                SelectionPath.Root,
                doc);
        }
    }
}

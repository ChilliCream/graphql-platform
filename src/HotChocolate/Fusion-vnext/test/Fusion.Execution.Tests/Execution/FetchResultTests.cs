using System.Text.Json;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Types;
using Xunit;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Unit tests for <see cref="FetchResult"/>.
/// </summary>
public sealed class FetchResultTests
{
    [Fact]
    public void GetFromSourceData_ReturnsRoot()
    {
        // arrange
        using var doc = JsonDocument.Parse(
            """
            {
              "data": { "hello": "world" }
            }
            """);

        var fetch = FetchResult.From(
            Path.Root,
            SelectionPath.Root,
            SelectionPath.Root,
            doc);

        // act
        var element = fetch.GetFromSourceData();

        // assert
        Assert.Equal(JsonValueKind.Object, element.ValueKind);
        Assert.Equal("world", element.GetProperty("hello").GetString());
    }

    [Fact]
    public void GetFromSourceData_User()
    {
        // arrange
        using var doc = JsonDocument.Parse(
            """
            {
              "data": {
                "user": { "id": 123, "name": "Luke" }
              }
            }
            """);

        var fetch = FetchResult.From(
            Path.Root,
            SelectionPath.Root,
            SelectionPath.Parse("user"),
            doc);

        // act
        var user = fetch.GetFromSourceData();

        // assert
        Assert.Equal(JsonValueKind.Object, user.ValueKind);
        Assert.True(user.TryGetProperty("id", out _));
    }

    [Fact]
    public void GetFromSourceData_InlineFragment()
    {
        // arrange
        using var doc = JsonDocument.Parse(
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
            """);

        var fetch = FetchResult.From(
            Path.Root,
            SelectionPath.Root,
            SelectionPath.Parse("node.<User>.id"),
            doc);

        // act
        var id = fetch.GetFromSourceData();

        // assert
        Assert.Equal(JsonValueKind.String, id.ValueKind);
        Assert.Equal("U1", id.GetString());
    }

    [Fact]
    public void GetFromSourceData_InlineFragment_Type_Mismatch()
    {
        // arrange
        using var doc = JsonDocument.Parse(
            """
            {
              "data": {
                "node": {
                  "__typename": "Group",
                  "id": "G1",
                  "name": "Ana"
                }
              }
            }
            """);

        var fetch = FetchResult.From(
            Path.Root,
            SelectionPath.Root,
            SelectionPath.Parse("node.<User>.id"),
            doc);

        // act
        var id = fetch.GetFromSourceData();

        // assert
        Assert.Equal(JsonValueKind.Undefined, id.ValueKind);
    }

    [Fact]
    public void GetFromSourceData_InlineFragment_Type_Missing()
    {
        // arrange
        using var doc = JsonDocument.Parse(
            """
            {
              "data": {
                "node": {
                  "id": "U1",
                  "name": "Ana"
                }
              }
            }
            """);

        var fetch = FetchResult.From(
            Path.Root,
            SelectionPath.Root,
            SelectionPath.Parse("node.<User>.id"),
            doc);

        // act
        var id = fetch.GetFromSourceData();

        // assert
        Assert.Equal(JsonValueKind.Undefined, id.ValueKind);
    }

    [Fact]
    public void GetFromSourceData_MissingPath_ReturnsDefault()
    {
        // arrange
        using var doc = JsonDocument.Parse(
            """
            { "data": { "foo": 1 } }
            """);

        var fetch = FetchResult.From(
            Path.Root,
            SelectionPath.Root,
            SelectionPath.Parse("bar.baz"),
            doc);

        // act
        var result = fetch.GetFromSourceData();

        // assert
        Assert.Equal(JsonValueKind.Undefined, result.ValueKind);
    }

    [Fact]
    public void CompareTo_SortsByRuntimePath()
    {
        // arrange
        var a = new TestBuilder("/foo");
        var b = new TestBuilder("/foo/bar");

        // act & assert
        Assert.True(a.FetchResult.CompareTo(b.FetchResult) < 0);
        Assert.True(b.FetchResult.CompareTo(a.FetchResult) > 0);
    }

    private sealed class TestBuilder
    {
        public FetchResult FetchResult { get; }

        public TestBuilder(string runtimePath)
        {
            // arrange
            using var doc = JsonDocument.Parse(
                """{ "data": {} }""");

            FetchResult = FetchResult.From(
                Path.Parse(runtimePath),
                SelectionPath.Root,
                SelectionPath.Root,
                doc);
        }
    }
}

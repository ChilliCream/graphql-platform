using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Execution;

public sealed class FetchResultStoreTests
{
    [Fact]
    public void Save_Root_Result()
    {
        // arrange
        var schema = new MutableSchemaDefinition();
        var store = new FetchResultStore(schema);

        using var doc = JsonDocument.Parse(
            """
            {
              "data": {
                "product": {
                  "id": "p1",
                  "sku": "hc-42"
                }
              }
            }
            """);

        // act
        store.Save(
            Path.Root,
            SelectionPath.Root,
            new SourceSchemaResult(Path.Root, doc));
    }

    [Fact]
    public void Save_Child_Result()
    {
        // arrange
        var schema = new MutableSchemaDefinition();
        var store = new FetchResultStore(schema);

        using var root = JsonDocument.Parse(
            """
            {
              "data": {
                "products": [{
                  "id": "p1",
                  "sku": "hc-42"
                }]
              }
            }
            """);

        using var child = JsonDocument.Parse(
            """
            {
              "data": {
                "productBySku": {
                  "sku": "hc-42",
                  "name": "Hot Chocolate",
                  "region": {
                    "name": "France"
                  }
                }
              }
            }
            """);

        store.Save(
            Path.Root,
            SelectionPath.Root,
            new SourceSchemaResult(Path.Root, root));

        // act
        store.Save(
            Path.Parse("/products[0]"),
            SelectionPath.Root.AppendField("productBySku"),
            new SourceSchemaResult(Path.Parse("/products[0]"), child));

        // assert
        var navigator = store.CreateNavigator();
        var data = store.CreateVariableValueSets(SelectionPath.Parse("products.region.name"), [], []);
    }

    /*

    [Fact]
    public void GetValues_ArrayFanOut_ReturnsRuntimePathPerArrayElement()
    {
        // arrange
        var store = new FetchResultStore();

        using var doc = JsonDocument.Parse(
            """
            {
              "data": {
                "products": [
                  { "id":"p1", "sku":"hc-42" },
                  { "id":"p2", "sku":"bs-13" }
                ]
              }
            }
            """);

        var fetch = FetchResult.From(
            Path.Root.Append("products"),
            SelectionPath.Parse("products"),
            SelectionPath.Parse("products"),
            doc);

        store.AddResult(fetch);

        var root = SelectionPath.Parse("products");
        var requirements = ImmutableArray.Create((
            Key: "sku",
            Map: FieldPath.Parse("sku")));

        // act
        var tuples = store.GetValues(root, requirements).OrderBy(t => t.Path).ToList();

        // assert
        Assert.Equal(2, tuples.Count);

        Assert.Equal("/products[0]", tuples[0].Path.ToString());
        new ObjectValueNode(tuples[0].Fields).MatchInlineSnapshot(
            """{ sku: "hc-42" }""");

        Assert.Equal("/products[1]", tuples[1].Path.ToString());
        new ObjectValueNode(tuples[1].Fields).MatchInlineSnapshot(
            """{ sku: "bs-13" }""");
    }

    [Fact]
    public void GetValues_SourcePath_DifferentAlias_ResolvesCorrectly()
    {
        // arrange
        var store = new FetchResultStore();

        using var doc = JsonDocument.Parse(
            """
            {
              "data": {
                "prods": [
                  { "id":"p1", "sku":"hc-42" },
                  { "id":"p2", "sku":"bs-13" }
                ]
              }
            }
            """);

        var fetch = FetchResult.From(
            Path.Root.Append("products"),
            SelectionPath.Parse("products"),
            SelectionPath.Parse("prods"),
            doc);

        store.AddResult(fetch);

        var root = SelectionPath.Parse("products");
        var requirements = ImmutableArray.Create((
            Key: "sku",
            Map: FieldPath.Parse("sku")));

        // act
        var tuples = store.GetValues(root, requirements).OrderBy(t => t.Path).ToList();

        // assert
        Assert.Equal(2, tuples.Count);

        Assert.Equal("/products[0]", tuples[0].Path.ToString());
        new ObjectValueNode(tuples[0].Fields).MatchInlineSnapshot(
            """{ sku: "hc-42" }""");

        Assert.Equal("/products[1]", tuples[1].Path.ToString());
        new ObjectValueNode(tuples[1].Fields).MatchInlineSnapshot(
            """{ sku: "bs-13" }""");
    }

    [Fact]
    public void GetValues_SourcePath_TwoLevelAlias_ResolvesCorrectly()
    {
        // arrange
        var store = new FetchResultStore();

        using var doc = JsonDocument.Parse(
            """
            {
              "data": {
                "prods": {
                  "items": [
                    { "id":"p1", "sku":"hc-42" },
                    { "id":"p2", "sku":"bs-13" }
                  ]
                }
              }
            }
            """);

        var fetch = FetchResult.From(
            Path.Root.Append("products"),
            SelectionPath.Parse("products"),
            SelectionPath.Parse("prods.items"),
            doc);

        store.AddResult(fetch);

        var root = SelectionPath.Parse("products");
        var requirements = ImmutableArray.Create((
            Key: "sku",
            Map: FieldPath.Parse("sku")));

        // act
        var tuples = store.GetValues(root, requirements).OrderBy(t => t.Path).ToList();

        // assert
        Assert.Equal(2, tuples.Count);

        Assert.Equal("/products[0]", tuples[0].Path.ToString());
        new ObjectValueNode(tuples[0].Fields).MatchInlineSnapshot(
            """{ sku: "hc-42" }""");

        Assert.Equal("/products[1]", tuples[1].Path.ToString());
        new ObjectValueNode(tuples[1].Fields).MatchInlineSnapshot(
            """{ sku: "bs-13" }""");
    }

    [Fact]
    public void GetValues_RootDeeperThanTarget_FanOutOnNestedObject()
    {
        // arrange
        var store = new FetchResultStore();

        using var doc = JsonDocument.Parse(
            """
            {
              "data": {
                "prods": {
                  "items": [
                    { "obj": { "id":"p1", "sku":"hc-42" } },
                    { "obj": { "id":"p2", "sku":"bs-13" } }
                  ]
                }
              }
            }
            """);

        var fetch = FetchResult.From(
            Path.Root.Append("products"),
            SelectionPath.Parse("products"),
            SelectionPath.Parse("prods.items"),
            doc);

        store.AddResult(fetch);

        var root = SelectionPath.Parse("products.obj");
        var requirements = ImmutableArray.Create((Key: "sku", Map: FieldPath.Parse("sku")));

        // act
        var values = store.GetValues(root, requirements).OrderBy(t => t.Path).ToList();

        // assert
        Assert.Equal(2, values.Count);

        Assert.Equal("/products[0]/obj", values[0].Path.ToString());
        new ObjectValueNode(values[0].Fields).MatchInlineSnapshot(
            """{ sku: "hc-42" }""");

        Assert.Equal("/products[1]/obj", values[1].Path.ToString());
        new ObjectValueNode(values[1].Fields).MatchInlineSnapshot(
            """{ sku: "bs-13" }""");
    }

    [Fact]
    public void GetValues_ArrayWithNullElements_SkipsNulls()
    {
        // arrange
        var store = new FetchResultStore();

        using var doc = JsonDocument.Parse(
            """
            {
              "data": {
                "products": [
                  null,
                  { "id":"p1", "sku":"hc-42" }
                ]
              }
            }
            """);

        var fetch = FetchResult.From(
            Path.Root.Append("products"),
            SelectionPath.Parse("products"),
            SelectionPath.Parse("products"),
            doc);

        store.AddResult(fetch);

        var root = SelectionPath.Parse("products");
        var requirements = ImmutableArray.Create((Key: "sku", Map: FieldPath.Parse("sku")));

        // act
        var tuples = store.GetValues(root, requirements).ToList();

        // assert
        Assert.Single(tuples);

        Assert.Equal("/products[1]", tuples[0].Path.ToString());
        new ObjectValueNode(tuples[0].Fields).MatchInlineSnapshot(
            """{ sku: "hc-42" }""");
    }

    [Fact]
    public void GetValues_MissingRequirementField_ReturnsNullValueNode()
    {
        // arrange
        var store = new FetchResultStore();

        using var doc = JsonDocument.Parse(
            """
            {
              "data": {
                "product": { "id": "p1" }
              }
            }
            """);

        var fetch = FetchResult.From(
            Path.Root.Append("product"),
            SelectionPath.Parse("product"),
            SelectionPath.Parse("product"),
            doc);

        store.AddResult(fetch);

        var root = SelectionPath.Parse("product");
        var requirements = ImmutableArray.Create((Key: "sku", Map: FieldPath.Parse("sku")));

        // act
        var tuples = store.GetValues(root, requirements).ToList();

        // assert
        var valueNode = tuples[0].Fields[0].Value;
        Assert.IsType<NullValueNode>(valueNode);
        Assert.Equal("null", valueNode.ToString());
    }

    [Fact]
    public void GetValues_InlineFragmentDiscriminator_FiltersByTypename()
    {
        // arrange
        var store = new FetchResultStore();

        using var doc = JsonDocument.Parse(
            """
            {
              "data": {
                "nodes": [
                  { "__typename":"User", "id":"u1", "sku":"u-01" },
                  { "__typename":"Group", "id":"g1", "sku":"g-01" }
                ]
              }
            }
            """);

        var fetch = FetchResult.From(
            Path.Root.Append("nodes"),
            SelectionPath.Parse("nodes"),
            SelectionPath.Parse("nodes"),
            doc);

        store.AddResult(fetch);

        var root = SelectionPath.Parse("nodes.<User>");
        var requirements = ImmutableArray.Create((Key: "sku", Map: FieldPath.Parse("sku")));

        // act
        var tuples = store.GetValues(root, requirements).ToList();

        // assert
        Assert.Single(tuples);

        Assert.Equal("/nodes[0]", tuples[0].Path.ToString());
        new ObjectValueNode(tuples[0].Fields).MatchInlineSnapshot(
            """{ sku: "u-01" }""");
    }

    */
}

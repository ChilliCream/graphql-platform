using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Transport.Formatters;

namespace HotChocolate.Fusion.Text.Json;

public class CompositeResultDocumentTests : FusionTestBase
{
    [Fact]
    public void Initialize_Basic_Result()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        // act
        var compositeResult = new CompositeResultDocument(plan.Operation, 0);

        // assert
        Assert.Equal(1, compositeResult.Data.GetPropertyCount());
        Assert.NotNull(compositeResult.Data.SelectionSet);

        var propertyValue = compositeResult.Data.GetProperty("productBySlug");
        Assert.Equal("productBySlug", propertyValue.GetPropertyName());
        Assert.Equal(JsonValueKind.Undefined, propertyValue.ValueKind);
    }

    [Fact]
    public void Add_Object_From_SelectionSet()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        var compositeResult = new CompositeResultDocument(plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var productBySlug = compositeResult.Data.GetProperty("productBySlug");
        var productBySlugSelection = productBySlug.AssertSelection();
        Assert.Equal("productBySlug", productBySlug.GetPropertyName());
        Assert.Equal(JsonValueKind.Undefined, productBySlug.ValueKind);
        Assert.False(productBySlugSelection.IsLeaf);

        // act
        var selectionSet = operation.GetSelectionSet(productBySlugSelection);
        productBySlug.SetObjectValue(selectionSet);

        // assert
        Assert.Equal(JsonValueKind.Object, productBySlug.ValueKind);
    }

    [Fact]
    public void Add_SourceResult_Leaf_Value()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        var compositeResult = new CompositeResultDocument(plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var productBySlug = compositeResult.Data.GetProperty("productBySlug");
        var productBySlugSelection = productBySlug.AssertSelection();
        var selectionSet = operation.GetSelectionSet(productBySlugSelection);
        productBySlug.SetObjectValue(selectionSet);

        var result =
            """
            {
              "id": 1,
              "name": "Abc"
            }
            """u8.ToArray();
        var sourceResult = SourceResultDocument.Parse(result, result.Length);

        // act
        productBySlug.GetProperty("id").SetLeafValue(sourceResult.Root.GetProperty("id"));
        productBySlug.GetProperty("name").SetLeafValue(sourceResult.Root.GetProperty("name"));

        // assert
        Assert.Equal(JsonValueKind.Number, productBySlug.GetProperty("id").ValueKind);
        Assert.Equal(JsonValueKind.String, productBySlug.GetProperty("name").ValueKind);
    }

    [Fact]
    public void Invalidate_Object()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        var compositeResult = new CompositeResultDocument(plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var productBySlug = compositeResult.Data.GetProperty("productBySlug");
        var productBySlugSelection = productBySlug.AssertSelection();
        var selectionSet = operation.GetSelectionSet(productBySlugSelection);
        productBySlug.SetObjectValue(selectionSet);

        // act
        productBySlug.Invalidate();

        // assert
        Assert.True(productBySlug.IsInvalidated);
    }

    [Fact]
    public void Invalidate_Data()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        var compositeResult = new CompositeResultDocument(plan.Operation, 0);

        // act
        compositeResult.Data.Invalidate();

        // assert
        Assert.True(compositeResult.Data.IsInvalidated);
    }

    [Fact]
    public void Enumerate_Object()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        var compositeResult = new CompositeResultDocument(plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var productBySlug = compositeResult.Data.GetProperty("productBySlug");
        var productBySlugSelection = productBySlug.AssertSelection();
        var selectionSet = operation.GetSelectionSet(productBySlugSelection);
        productBySlug.SetObjectValue(selectionSet);

        var result =
            """
                {
                  "id": 1,
                  "name": "Abc"
                }
                """u8.ToArray();

        var sourceResult = SourceResultDocument.Parse(result, result.Length);
        productBySlug.GetProperty("id").SetLeafValue(sourceResult.Root.GetProperty("id"));
        productBySlug.GetProperty("name").SetLeafValue(sourceResult.Root.GetProperty("name"));

        // act
        var enumerator = productBySlug.EnumerateObject();

        // assert
        Assert.True(enumerator.MoveNext());
        Assert.Equal("id", enumerator.Current.Name);
        Assert.Equal(1, enumerator.Current.Value.GetInt32());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("name", enumerator.Current.Name);
        Assert.Equal("Abc", enumerator.Current.Value.AssertString());

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Enumerate_Array()
    {
        // arrange
        var schema = ComposeShoppingSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                users {
                    nodes {
                        name
                    }
                }
            }
            """);

        var compositeResult = new CompositeResultDocument(plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var users = compositeResult.Data.GetProperty("users");
        var usersSelection = users.AssertSelection();
        var usersSelectionSet = operation.GetSelectionSet(usersSelection);
        users.SetObjectValue(usersSelectionSet);

        var nodes = users.GetProperty("nodes");
        var nodesSelection = nodes.AssertSelection();
        var nodesSelectionSet = operation.GetSelectionSet(nodesSelection);
        nodes.SetArrayValue(3);

        var result =
            """
                {
                  "name1": "Abc",
                  "name2": "Def",
                  "name3": "Ghi"
                }
                """u8.ToArray();

        var sourceResult = SourceResultDocument.Parse(result, result.Length);
        var i = 0;

        // act
        foreach (var element in nodes.EnumerateArray())
        {
            element.SetObjectValue(nodesSelectionSet);
            var name = element.GetProperty("name");
            name.SetLeafValue(sourceResult.Root.GetProperty("name" + ++i));
        }

        // assert
        using var enumerator = nodes.EnumerateArray().GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal("Abc", enumerator.Current.GetProperty("name").AssertString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("Def", enumerator.Current.GetProperty("name").AssertString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("Ghi", enumerator.Current.GetProperty("name").AssertString());

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Path_Fields_Only()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        var compositeResult = new CompositeResultDocument(plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var productBySlug = compositeResult.Data.GetProperty("productBySlug");
        var productBySlugSelection = productBySlug.AssertSelection();
        var selectionSet = operation.GetSelectionSet(productBySlugSelection);
        productBySlug.SetObjectValue(selectionSet);

        var result =
            """
                {
                  "id": 1,
                  "name": "Abc"
                }
                """u8.ToArray();

        var sourceResult = SourceResultDocument.Parse(result, result.Length);
        productBySlug.GetProperty("id").SetLeafValue(sourceResult.Root.GetProperty("id"));
        productBySlug.GetProperty("name").SetLeafValue(sourceResult.Root.GetProperty("name"));

        // act
        var path = productBySlug.GetProperty("name").Path;

        // assert
        Assert.Equal("/productBySlug/name", path.ToString());
    }

    [Fact]
    public void Path_Array_Index()
    {
        // arrange
        var schema = ComposeShoppingSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                users {
                    nodes {
                        name
                    }
                }
            }
            """);

        var compositeResult = new CompositeResultDocument(plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var users = compositeResult.Data.GetProperty("users");
        var usersSelection = users.AssertSelection();
        var usersSelectionSet = operation.GetSelectionSet(usersSelection);
        users.SetObjectValue(usersSelectionSet);

        var nodes = users.GetProperty("nodes");
        var nodesSelection = nodes.AssertSelection();
        var nodesSelectionSet = operation.GetSelectionSet(nodesSelection);
        nodes.SetArrayValue(1);

        var element = nodes[0];
        element.SetObjectValue(nodesSelectionSet);

        var name = element.GetProperty("name");

        var result =
            """
                {
                  "name": "Abc"
                }
                """u8.ToArray();

        var sourceResult = SourceResultDocument.Parse(result, result.Length);
        name.SetLeafValue(sourceResult.Root.GetProperty("name"));

        // act
        var path = name.Path;

        // assert
        Assert.Equal("/users/nodes[0]/name", path.ToString());
    }

    [Fact]
    public void Write_Document_To_BufferWriter()
    {
        // arrange
        using var buffer = new PooledArrayWriter();

        var schema = ComposeShoppingSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                users {
                    nodes {
                        name
                    }
                }
            }
            """);

        var compositeResult = new CompositeResultDocument(plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var users = compositeResult.Data.GetProperty("users");
        var usersSelection = users.AssertSelection();
        var usersSelectionSet = operation.GetSelectionSet(usersSelection);
        users.SetObjectValue(usersSelectionSet);

        var nodes = users.GetProperty("nodes");
        var nodesSelection = nodes.AssertSelection();
        var nodesSelectionSet = operation.GetSelectionSet(nodesSelection);
        nodes.SetArrayValue(3);

        var result =
            """
                {
                  "name1": "Abc",
                  "name2": "Def",
                  "name3": "Ghi"
                }
                """u8.ToArray();

        var sourceResult = SourceResultDocument.Parse(result, result.Length);
        var i = 0;

        foreach (var element in nodes.EnumerateArray())
        {
            element.SetObjectValue(nodesSelectionSet);
            var name = element.GetProperty("name");
            name.SetLeafValue(sourceResult.Root.GetProperty("name" + ++i));
        }

        // act
        var operationResultData = new OperationResultData(
            compositeResult,
            compositeResult.Data.IsNullOrInvalidated,
            compositeResult,
            compositeResult);
        var operationResult = new OperationResult(
            operationResultData);

        new JsonResultFormatter(indented: true).Format(operationResult, buffer);

        // assert
        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        json.MatchSnapshot();
    }

    [Fact]
    public async Task Write_Document_To_PipeWriter()
    {
        // arrange
        var schema = ComposeShoppingSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                users {
                    nodes {
                        name
                    }
                }
            }
            """);

        var compositeResult = new CompositeResultDocument(plan.Operation, 0);
        var operation = compositeResult.Data.Operation;

        var users = compositeResult.Data.GetProperty("users");
        var usersSelection = users.AssertSelection();
        var usersSelectionSet = operation.GetSelectionSet(usersSelection);
        users.SetObjectValue(usersSelectionSet);

        var nodes = users.GetProperty("nodes");
        var nodesSelection = nodes.AssertSelection();
        var nodesSelectionSet = operation.GetSelectionSet(nodesSelection);
        nodes.SetArrayValue(3);

        var result =
            """
                {
                  "name1": "Abc",
                  "name2": "Def",
                  "name3": "Ghi"
                }
                """u8.ToArray();

        var sourceResult = SourceResultDocument.Parse(result, result.Length);
        var i = 0;

        foreach (var element in nodes.EnumerateArray())
        {
            element.SetObjectValue(nodesSelectionSet);
            var name = element.GetProperty("name");
            name.SetLeafValue(sourceResult.Root.GetProperty("name" + ++i));
        }

        // act
        await using var memoryStream = new MemoryStream();
        var writer = PipeWriter.Create(memoryStream);
        var operationResultData = new OperationResultData(
            compositeResult,
            compositeResult.Data.IsNullOrInvalidated,
            compositeResult,
            compositeResult);
        var operationResult = new OperationResult(
            operationResultData);

        new JsonResultFormatter(indented: true).Format(operationResult, writer);

        await writer.FlushAsync();
        await writer.CompleteAsync();

        // assert
        var json = Encoding.UTF8.GetString(memoryStream.ToArray());
        json.MatchSnapshot();
    }
}

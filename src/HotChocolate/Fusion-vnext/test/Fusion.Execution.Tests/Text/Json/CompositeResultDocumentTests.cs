using System.Text;
using System.Text.Json;

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
    public void Invalidate_Scalar_Throws()
    {
        // arrange
        var schema = CreateCompositeSchema();

        var plan = PlanOperation(
            schema,
            """
            {
                __typename
            }
            """);

        var compositeResult = new CompositeResultDocument(plan.Operation, 0);

        var typeName = compositeResult.Data.GetProperty("__typename");

        // act + assert
        Assert.Throws<InvalidOperationException>(() => typeName.Invalidate());
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
}

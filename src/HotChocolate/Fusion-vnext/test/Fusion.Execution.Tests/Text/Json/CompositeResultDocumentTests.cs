using System.Collections.Specialized;
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
        var compositeResult = new CompositeResultDocument(plan.Operation);

        // assert
        Assert.Equal(1, compositeResult.Data.GetPropertyCount());
        Assert.NotNull(compositeResult.Data.SelectionSet);

        var propertyValue = compositeResult.Data.GetProperty("productBySlug");
        Assert.Equal("productBySlug", propertyValue.GetPropertyName());
        Assert.Equal(JsonValueKind.Undefined, propertyValue.ValueKind);
    }

    [Fact]
    public void Add_SourceSchema_Value()
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
        var compositeResult = new CompositeResultDocument(plan.Operation);
        var operation = compositeResult.Data.Operation;

        var productBySlug = compositeResult.Data.GetProperty("productBySlug");
        var productBySlugSelection = productBySlug.GetRequiredSelection();
        Assert.Equal("productBySlug", productBySlug.GetPropertyName());
        Assert.Equal(JsonValueKind.Undefined, productBySlug.ValueKind);
        Assert.False(productBySlugSelection.IsLeaf);

        var selectionSet = operation.GetSelectionSet(productBySlugSelection);
        productBySlug.SetValue(selectionSet);
        Assert.Equal(JsonValueKind.Object, productBySlug.ValueKind);
    }
}

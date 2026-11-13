using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Text.Json;

public class ResultDocumentParentTests
{
    [Fact]
    public void Parent_Should_ReturnNulledObjectValue_When_ObjectUnderPropertyWasNulled()
    {
        // arrange
        var operation = CreateOperation("{ item { a b c } }");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var item = document.Data.GetProperty("item");
        item.SetObjectValue(GetItemSelectionSet(operation, item));
        var c = item.GetProperty("c");
        item.SetNullValue();

        // act
        var parent = c.Parent;

        // assert
        Assert.Equal(JsonValueKind.Null, parent.ValueKind);
        Assert.Equal("item", parent.Path.Print());
    }

    [Fact]
    public void Parent_Should_ReturnNulledObjectValue_When_ObjectUnderListElementWasNulled()
    {
        // arrange
        var operation = CreateOperation("{ items { a b c } }");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var items = document.Data.GetProperty("items");
        items.SetArrayValue(1);
        var element = items[0];
        element.SetObjectValue(GetItemSelectionSet(operation, items));
        var c = element.GetProperty("c");
        element.SetNullValue();

        // act
        var parent = c.Parent;

        // assert
        Assert.Equal(JsonValueKind.Null, parent.ValueKind);
        Assert.Equal("items[0]", parent.Path.Print());
    }

    private static SelectionSet GetItemSelectionSet(Operation operation, ResultElement element)
    {
        var selection = element.AssertSelection();
        var itemType = (ObjectType)selection.Type.NamedType();
        return operation.GetSelectionSet(selection, itemType);
    }

    private static Operation CreateOperation(string query)
    {
        var schema =
            SchemaBuilder.New()
                .AddDocumentFromString(
                    """
                    type Query {
                      item: Item
                      items: [Item]
                    }

                    type Item {
                      a: String!
                      b: String!
                      c: String!
                    }
                    """)
                .Use(_ => _)
                .Create();

        return OperationCompiler.Compile("abc", Utf8GraphQLParser.Parse(query), schema);
    }
}

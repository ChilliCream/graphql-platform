using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Text.Json;

public class ResultDocumentFieldTests
{
    [Fact]
    public void SetObjectValue_Should_PairSelectionsWithValues_When_ObjectIsAPropertyValue()
    {
        // arrange
        var operation = CreateOperation("{ item { a b c } }");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var item = document.Data.GetProperty("item");

        // act
        var fields = item.SetObjectValue(GetItemSelectionSet(operation, item));

        while (fields.MoveNext())
        {
            fields.Value.SetStringValue(fields.Selection.ResponseName.ToUpperInvariant());
        }

        // assert
        document.Data.MatchInlineSnapshot(
            """
            {
              "item": {
                "a": "A",
                "b": "B",
                "c": "C"
              }
            }
            """);
    }

    [Fact]
    public void SetObjectValue_Should_PairSelectionsWithValues_When_ObjectIsAListElement()
    {
        // arrange
        var operation = CreateOperation("{ items { c a } }");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var items = document.Data.GetProperty("items");
        items.SetArrayValue(2);

        // act
        for (var i = 0; i < 2; i++)
        {
            var fields = items[i].SetObjectValue(GetItemSelectionSet(operation, items));

            while (fields.MoveNext())
            {
                fields.Value.SetStringValue(fields.Selection.ResponseName + i);
            }
        }

        // assert
        document.Data.MatchInlineSnapshot(
            """
            {
              "items": [
                {
                  "c": "c0",
                  "a": "a0"
                },
                {
                  "c": "c1",
                  "a": "a1"
                }
              ]
            }
            """);
    }

    [Fact]
    public void SetObjectValue_Should_SkipExcludedSelections_When_IncludeConditionIsNotMet()
    {
        // arrange
        var operation = CreateOperation("query($x: Boolean!) { item { a b @include(if: $x) c } }");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var item = document.Data.GetProperty("item");
        var responseNames = new List<string>();

        // act
        var fields = item.SetObjectValue(GetItemSelectionSet(operation, item));

        while (fields.MoveNext())
        {
            responseNames.Add(fields.Selection.ResponseName);
            fields.Value.SetStringValue(fields.Selection.ResponseName);
        }

        // assert
        Assert.Equal(["a", "c"], responseNames);
        Assert.Equal("c", item.GetProperty("c").GetString());
    }

    [Fact]
    public void SetObjectValue_Should_Throw_When_SelectionTypeIsNotComposite()
    {
        // arrange
        var operation = CreateOperation("{ name item { a } }");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var selectionSet = GetItemSelectionSet(operation, document.Data.GetProperty("item"));
        var name = document.Data.GetProperty("name");

        // act
        void Act() => name.SetObjectValue(selectionSet);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("The element is not a composite type.", exception.Message);
    }

    [Fact]
    public void SetObjectValue_Should_Throw_When_ListElementTypeIsNotComposite()
    {
        // arrange
        var operation = CreateOperation("{ names item { a } }");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var selectionSet = GetItemSelectionSet(operation, document.Data.GetProperty("item"));
        var names = document.Data.GetProperty("names");
        names.SetArrayValue(1);

        // act
        void Act() => names[0].SetObjectValue(selectionSet);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("The element is not a composite type.", exception.Message);
    }

    [Theory]
    [InlineData("item", true)]
    [InlineData("requiredItem", false)]
    [InlineData("requiredItems", true)]
    public void IsNullable_Should_ReflectTheSelectionType_When_ElementIsAPropertyValue(
        string fieldName,
        bool expected)
    {
        // arrange
        var operation = CreateOperation($"{{ {fieldName} {{ a }} }}");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var element = document.Data.GetProperty(fieldName);

        // act
        var isNullable = element.IsNullable;

        // assert
        Assert.Equal(expected, isNullable);
    }

    [Theory]
    [InlineData("items", true)]
    [InlineData("requiredItems", false)]
    public void IsNullable_Should_ReflectTheElementType_When_ElementIsAListElement(
        string fieldName,
        bool expected)
    {
        // arrange
        var operation = CreateOperation($"{{ {fieldName} {{ a }} }}");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var list = document.Data.GetProperty(fieldName);
        list.SetArrayValue(1);

        // act
        var isNullable = list[0].IsNullable;

        // assert
        Assert.Equal(expected, isNullable);
    }

    [Fact]
    public void IsNullable_Should_ReflectTheSelectionType_When_ElementIsAFieldOfAnObject()
    {
        // arrange
        var operation = CreateOperation("{ item { a c } }");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var item = document.Data.GetProperty("item");
        item.SetObjectValue(GetItemSelectionSet(operation, item));

        // act
        var a = item.GetProperty("a").IsNullable;
        var c = item.GetProperty("c").IsNullable;

        // assert
        Assert.False(a);
        Assert.True(c);
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
                      name: String
                      names: [String]
                      item: Item
                      requiredItem: Item!
                      items: [Item]
                      requiredItems: [Item!]
                    }

                    type Item {
                      a: String!
                      b: String!
                      c: String
                    }
                    """)
                .Use(_ => _)
                .Create();

        return OperationCompiler.Compile("abc", Utf8GraphQLParser.Parse(query), schema);
    }
}

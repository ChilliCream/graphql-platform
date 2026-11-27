using System.Buffers;
using System.Text;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Text.Json;

public class ResultDocumentObjectTests
{
    [Fact]
    public void SetObjectValue_Should_KeepEveryObjectIntact_When_ObjectsFillSeveralRowChunks()
    {
        // arrange
        // 400 objects of 8 rows each fill the first row chunks and reach their ends at varying rows
        const int count = 400;
        var operation = CreateOperation("{ items { a b c } }");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var items = document.Data.GetProperty("items");
        var selection = items.AssertSelection();
        var selectionSet = operation.GetSelectionSet(selection, (ObjectType)selection.Type.NamedType());
        var expected = new StringBuilder("{\"items\":[");

        for (var i = 0; i < count; i++)
        {
            expected.Append(i == 0 ? "" : ",");
            expected.Append($"{{\"a\":\"a{i}\",\"b\":\"b{i}\",\"c\":\"c{i}\"}}");
        }

        expected.Append("]}");

        // act
        items.SetArrayValue(count);

        for (var i = 0; i < count; i++)
        {
            var item = items[i];
            item.SetObjectValue(selectionSet);
            item.GetProperty("a").SetStringValue("a" + i);
            item.GetProperty("b").SetStringValue("b" + i);
            item.GetProperty("c").SetStringValue("c" + i);
        }

        // assert
        var buffer = new ArrayBufferWriter<byte>();
        document.Data.WriteTo(buffer);
        Assert.Equal(expected.ToString(), Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    public void SetObjectValue_Should_LinkEveryPropertyToItsObject_When_ObjectsFillSeveralRowChunks()
    {
        // arrange
        const int count = 400;
        var operation = CreateOperation("{ items { a b c } }");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var items = document.Data.GetProperty("items");
        var selection = items.AssertSelection();
        var selectionSet = operation.GetSelectionSet(selection, (ObjectType)selection.Type.NamedType());
        var expected = new string[count];
        var actual = new string[count];
        items.SetArrayValue(count);

        // act
        for (var i = 0; i < count; i++)
        {
            items[i].SetObjectValue(selectionSet);
            expected[i] = $"items[{i}].c c items[{i}]";
        }

        for (var i = 0; i < count; i++)
        {
            var property = items[i].GetProperty("c");
            var responseName = property.AssertSelection().ResponseName;
            actual[i] = $"{property.Path.Print()} {responseName} {property.Parent.Path.Print()}";
        }

        // assert
        Assert.Equal(expected, actual);
    }

    private static Operation CreateOperation(string query)
    {
        var schema =
            SchemaBuilder.New()
                .AddDocumentFromString(
                    """
                    type Query {
                      items: [Item]
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

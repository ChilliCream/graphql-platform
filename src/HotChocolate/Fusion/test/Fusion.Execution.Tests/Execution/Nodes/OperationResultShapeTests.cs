using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Nodes;

public class OperationResultShapeTests : FusionTestBase
{
    [Theory]
    [InlineData(
        "query($id: ID!) { userById(id: $id) { id reviews { body } } }",
        """{"data":{"userById":{"id":1,"reviews":[{"body":"a"},{"body":"b"}]}}}""",
        "- data userById userById.reviews userById.reviews")]
    [InlineData(
        "{ users { id name } topProducts { upc } }",
        """{"data":{"users":[{"id":1,"name":"a"},{"name":"b","id":2}],"topProducts":[{"upc":"1"}]}}""",
        "- data users - topProducts")]
    [InlineData(
        "{ user: userById(id: 1) { key: id } }",
        """{"data":{"user":{"key":1}}}""",
        "- data user")]
    [InlineData(
        "{ node(id: 1) { id ... on User { profile { name } } } }",
        """{"data":{"node":{"id":1,"profile":{"name":"a"}}}}""",
        "- data - -")]
    [InlineData(
        "{ user { id id name } }",
        """{"data":{"user":{"id":1,"name":"a"}}}""",
        "- data -")]
    public void Create_Should_IdentifyTheObjectsOfASelectionSet_When_TheResultIsOfTheOperationItself(
        string operation,
        string result,
        string expectedObjects)
    {
        // arrange
        var shape = OperationResultShape.Create(Encoding.UTF8.GetBytes(operation));
        using var arena = new MemoryArena();

        // act
        using var document = Parse(arena, result, shape.Single);

        // assert
        Assert.Equal(expectedObjects, DescribeIdentifiedObjects(document));
    }

    [Fact]
    public void Create_Should_IdentifyTheValueOfEveryDataProperty_When_TheOperationIsSentOncePerVariableSet()
    {
        // arrange
        var shape = OperationResultShape.Create("query($id: ID!) { userById(id: $id) { id reviews { body } } }"u8);
        using var arena = new MemoryArena();

        // act
        using var document = Parse(
            arena,
            """{"data":{"a_0_userById":{"id":1,"reviews":[{"body":"a"}]},"a_1_userById":null,"a_2_userById":{"id":2,"reviews":[]}}}""",
            shape.AliasBatch);

        // assert
        Assert.Equal(
            "- - a_0_userById a_0_userById.reviews a_2_userById",
            DescribeIdentifiedObjects(document));
    }

    [Theory]
    [InlineData("{ users { id } topProducts { upc } }")]
    [InlineData("{ version }")]
    [InlineData("{ ... on Query { users { id } } }")]
    public void Create_Should_NotExpectAnAliasBatch_When_TheOperationHasNoSingleRootFieldWithSelections(
        string operation)
    {
        // act
        var shape = OperationResultShape.Create(Encoding.UTF8.GetBytes(operation));

        // assert
        Assert.Null(shape.AliasBatch);
    }

    [Fact]
    public void GetSelections_Should_ReturnTheSelectionOfEveryExpectedField_When_ResponseNamesMatch()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: test
            type Query {
              item: Item
            }
            type Item {
              a: String
              b: Int
              c: Child
            }
            type Child {
              x: String
            }
            """);
        var plan = PlanOperation(schema, "{ item { c { x } a } }");
        var itemSelectionSet = plan.Operation.GetSelectionSet(plan.Operation.RootSelectionSet.Selections[0]);
        var shape = OperationResultShape.Create("{ item { a b c { x } } }"u8).Single;
        using var arena = new MemoryArena();
        using var document = Parse(arena, """{"data":{"item":{"a":"1","b":2,"c":{"x":"3"}}}}""", shape);
        var item = document.Root.GetProperty("data"u8).GetProperty("item"u8).CreateSnapshot();

        // act
        var selections = item.GetPropertySelections(itemSelectionSet);

        // assert
        Assert.Equal(new[] { "a", null, "c" }, selections?.Select(t => t?.ResponseName).ToArray());
        Assert.Same(selections, item.GetPropertySelections(itemSelectionSet));
    }

    private static SourceResultDocument Parse(MemoryArena arena, string json, SourceResultShape? shape)
    {
        using var writer = new ArenaBufferWriter(arena);
        var bytes = Encoding.UTF8.GetBytes(json);

        bytes.CopyTo(writer.GetSpan(bytes.Length));
        writer.Advance(bytes.Length);
        return SourceResultDocument.ParseFilled(arena, writer.Segments, writer.UsedChunks, writer.LastLength, shape);
    }

    /// <summary>
    /// Describes every object of <paramref name="document"/> in document order: an identified
    /// object by the property names that lead to it below <c>data</c>, any other object by <c>-</c>.
    /// </summary>
    private static string DescribeIdentifiedObjects(SourceResultDocument document)
    {
        var objects = new List<string>();

        Visit(document.Root, "$");
        return string.Join(' ', objects);

        void Visit(SourceResultElement element, string path)
        {
            if (element.ValueKind is JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    Visit(item, path);
                }
            }
            else if (element.ValueKind is JsonValueKind.Object)
            {
                var snapshot = element.CreateSnapshot();
                var isIdentified = document.GetObjectShapeNode(snapshot._cursor, snapshot._row) >= 0;

                objects.Add(isIdentified ? path : "-");

                foreach (var property in element.EnumerateObject())
                {
                    Visit(property.Value, path is "$" or "data" ? property.Name : $"{path}.{property.Name}");
                }
            }
        }
    }
}

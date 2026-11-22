using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Clients.AliasBatching;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

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
        var shape = CreateShape(operation);
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
        var shape = CreateShape("query($id: ID!) { userById(id: $id) { id reviews { body } } }");
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

    [Fact]
    public void GetMergedAliasBatch_Should_IdentifyTheObjectsOfEveryOperation_When_RequestsAreMergedIntoOneBatch()
    {
        // arrange
        var users = CreateRequest("query($id: ID!) { user(id: $id) { reviews { id body } } }");
        var products = CreateRequest("query($upc: ID!) { product(upc: $upc) { upc reviews { id } } }");
        SourceSchemaClientRequest[] requests = [users, products, users];
        AliasBatchItem[] items = [CreateItem(0), CreateItem(1), CreateItem(2), CreateItem(2)];
        using var arena = new MemoryArena();

        // act
        var shape = users.ResultShape!.GetMergedAliasBatch(requests, items);
        using var document = Parse(
            arena,
            """{"data":{"_0_user":{"reviews":[{"id":1,"body":"a"}]},"_1_product":{"upc":"1","reviews":[{"id":2}]},"_2_user":{"reviews":[]},"_3_other":{"upc":"2"}}}""",
            shape);

        // assert
        Assert.Equal(
            "- - _0_user _0_user.reviews _1_product _1_product.reviews _2_user -",
            DescribeIdentifiedObjects(document));
        Assert.Same(shape, users.ResultShape.GetMergedAliasBatch(requests, items));
    }

    [Fact]
    public void GetMergedAliasBatch_Should_IgnoreARequest_When_NoItemOfTheBatchBelongsToIt()
    {
        // arrange
        var users = CreateRequest("query($id: ID!) { user(id: $id) { id } }");
        var products = CreateRequest("query($upc: ID!) { product(upc: $upc) { upc } }");
        var unbatched = CreateRequest("{ user(id: 1) { name } }") with { ResultShape = null };
        SourceSchemaClientRequest[] requests = [users, unbatched, products];
        AliasBatchItem[] items = [CreateItem(0), CreateItem(2)];
        using var arena = new MemoryArena();

        // act
        var shape = users.ResultShape!.GetMergedAliasBatch(requests, items);
        using var document = Parse(arena, """{"data":{"_0_user":{"id":1},"_1_product":{"upc":"1"}}}""", shape);

        // assert
        Assert.Equal("- - _0_user _1_product", DescribeIdentifiedObjects(document));
    }

    [Fact]
    public void GetMergedAliasBatch_Should_ExpectNothing_When_TwoOperationsHaveTheSameRootField()
    {
        // arrange
        var first = CreateRequest("query($id: ID!) { user(id: $id) { id } }");
        var second = CreateRequest("query($id: ID!) { user(id: $id) { name } }");

        // act
        var shape = first.ResultShape!.GetMergedAliasBatch([first, second], [CreateItem(0), CreateItem(1)]);

        // assert
        Assert.Null(shape);
    }

    [Fact]
    public void GetMergedAliasBatch_Should_ExpectNothing_When_ARootFieldReadsAsTheAliasOfAnItem()
    {
        // arrange
        // the second item of the batch gets the alias _1_b, which is the root field of the first one
        var first = CreateRequest("{ _1_b { x } }");
        var second = CreateRequest("{ b { y } }");

        // act
        var shape = first.ResultShape!.GetMergedAliasBatch([first, second], [CreateItem(0), CreateItem(1)]);

        // assert
        Assert.Null(shape);
    }

    [Fact]
    public void GetMergedAliasBatch_Should_IdentifyTheObjectsOfEveryOperation_When_PropertiesAreNotInRequestOrder()
    {
        // arrange
        var requests = CreateRequestsWithDistinctRootFields(32);
        var items = Enumerable.Range(0, requests.Length).Select(CreateItem).ToArray();
        using var arena = new MemoryArena();

        // act
        var shape = requests[0].ResultShape!.GetMergedAliasBatch(requests, items);
        using var document = Parse(
            arena,
            """{"data":{"_31_f31":{"id":1},"_0_f0":{"id":2},"_5_f5":{"id":3},"_4_f4":{"id":4},"_4_f4x":{"id":5}}}""",
            shape);

        // assert
        Assert.Equal("- - _31_f31 _0_f0 _5_f5 _4_f4 -", DescribeIdentifiedObjects(document));
    }

    [Fact]
    public void GetMergedAliasBatch_Should_ExpectNothing_When_MoreThanThirtyTwoRequestsAreMerged()
    {
        // arrange
        var requests = CreateRequestsWithDistinctRootFields(33);
        var items = Enumerable.Range(0, requests.Length).Select(CreateItem).ToArray();

        // act
        var shape = requests[0].ResultShape!.GetMergedAliasBatch(requests, items);

        // assert
        Assert.Null(shape);
    }

    [Fact]
    public void GetMergedAliasBatch_Should_ExpectNothing_When_EightOtherBatchesAreKept()
    {
        // arrange
        var first = CreateRequest("{ a { id } }");
        var others = CreateRequestsWithDistinctRootFields(9);
        AliasBatchItem[] items = [CreateItem(0), CreateItem(1)];
        var shapes = new SourceResultShape?[others.Length];

        // act
        for (var i = 0; i < others.Length; i++)
        {
            shapes[i] = first.ResultShape!.GetMergedAliasBatch([first, others[i]], items);
        }

        // assert
        Assert.Equal(8, shapes.Count(t => t is not null));
        Assert.Null(shapes[8]);
        Assert.Same(shapes[0], first.ResultShape!.GetMergedAliasBatch([first, others[0]], items));
    }

    [Theory]
    [InlineData("{ users { id } topProducts { upc } }")]
    [InlineData("{ version }")]
    [InlineData("{ ... on Query { users { id } } }")]
    [InlineData("{ user: userById(id: 1) { id } }")]
    public void Create_Should_NotExpectAnAliasBatch_When_TheOperationHasNoSingleRootFieldWithSelections(
        string operation)
    {
        // act
        var shape = CreateShape(operation);

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
        var shape = CreateShape("{ item { a b c { x } } }").Single;
        using var arena = new MemoryArena();
        using var document = Parse(arena, """{"data":{"item":{"a":"1","b":2,"c":{"x":"3"}}}}""", shape);
        var item = document.Root.GetProperty("data"u8).GetProperty("item"u8).CreateSnapshot();

        // act
        var selections = item.GetPropertySelections(itemSelectionSet);

        // assert
        Assert.Equal(new[] { "a", null, "c" }, selections?.Select(t => t?.ResponseName).ToArray());
        Assert.Same(selections, item.GetPropertySelections(itemSelectionSet));
    }

    [Fact]
    public void GetSelections_Should_ReturnNull_When_SelectionsAreKeptForEightOtherSelectionSets()
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
            }
            """);
        var shape = CreateShape("{ item { a } }").Single;
        using var arena = new MemoryArena();
        using var document = Parse(arena, """{"data":{"item":{"a":"1"}}}""", shape);
        var item = document.Root.GetProperty("data"u8).GetProperty("item"u8).CreateSnapshot();
        var selectionSets = new SelectionSet[9];

        for (var i = 0; i < selectionSets.Length; i++)
        {
            var plan = PlanOperation(schema, "{ item { a } }");
            selectionSets[i] = plan.Operation.GetSelectionSet(plan.Operation.RootSelectionSet.Selections[0]);
        }

        var selections = new Selection?[]?[selectionSets.Length];

        // act
        for (var i = 0; i < selectionSets.Length; i++)
        {
            selections[i] = item.GetPropertySelections(selectionSets[i]);
        }

        // assert
        Assert.Equal(9, selectionSets.Distinct().Count());
        Assert.Equal(8, selections.Count(t => t is not null));
        Assert.Null(selections[8]);
        Assert.Same(selections[0], item.GetPropertySelections(selectionSets[0]));
    }

    private static SourceSchemaClientRequest CreateRequest(string operation)
    {
        var sourceText = Encoding.UTF8.GetBytes(operation);

        return new SourceSchemaClientRequest
        {
            Node = null!,
            SchemaName = "a",
            OperationType = OperationType.Query,
            OperationSourceText = new OperationSourceText(
                "Op",
                OperationType.Query,
                sourceText,
                OperationSourceTextHash.Compute(sourceText)),
            OperationDocument = Utf8GraphQLOperationParser.Parse(sourceText, ParserOptions.Trusted),
            ResultShape = CreateShape(operation)
        };
    }

    private static SourceSchemaClientRequest[] CreateRequestsWithDistinctRootFields(int count)
    {
        var requests = new SourceSchemaClientRequest[count];

        for (var i = 0; i < requests.Length; i++)
        {
            requests[i] = CreateRequest($"{{ f{i} {{ id }} }}");
        }

        return requests;
    }

    private static AliasBatchItem CreateItem(int requestIndex)
        => new(requestIndex, rootField: default, lookupTypeName: "Type", variables: default);

    private static OperationResultShape CreateShape(string operation)
        => OperationResultShape.Create(
            Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(operation), ParserOptions.Trusted));

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

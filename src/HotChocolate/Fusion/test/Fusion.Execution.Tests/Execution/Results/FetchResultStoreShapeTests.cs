using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Errors;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Results;

public sealed class FetchResultStoreShapeTests : FusionTestBase
{
    private const string Operation = "{ item { a b c { x y } d { x y } e } }";

    private static readonly FusionSchemaDefinition s_schema = ComposeSchema(
        """
        # name: test
        type Query {
          item: Item
          pet: Pet
        }
        type Item {
          a: String
          b: Int
          c: Child
          d: [Child]
          e: String
        }
        type Child {
          x: String
          y: Int
        }
        interface Pet {
          name: String
        }
        type Dog implements Pet {
          name: String
          barks: Boolean
        }
        type Cat implements Pet {
          name: String
          lives: Int
        }
        """);

    [Theory]
    [InlineData(
        """
        {"data":{"item":{"a":"first","b":5,"c":{"x":"c","y":1},"d":[{"x":"d1","y":2},{"x":"d2","y":3}],"e":"last"}}}
        """)]
    [InlineData(
        """
        {"data":{"item":{"e":"last","d":[{"y":2,"x":"d1"},{"x":"d2","y":3}],"c":{"y":1,"x":"c"},"b":5,"a":"first"}}}
        """)]
    [InlineData(
        """
        {"data":{"item":{"c":{"x":"c","y":1},"unknown":true,"a":"first","e":"last","d":[{"x":"d1","y":2},{"y":3,"x":"d2"}],"b":5}}}
        """)]
    [InlineData(
        """
        { "data" : { "item" : { "a" : "first", "b" : 5, "c" : { "x" : "c", "y" : 1 }, "d" : [ { "x" : "d1", "y" : 2 }, { "x" : "d2", "y" : 3 } ], "e" : "last" } } }
        """)]
    public void AddPartialResults_Should_CompleteEveryField_When_TheResultIsParsedWithItsExpectedShape(string payload)
    {
        // arrange
        var plan = PlanOperation(s_schema, Operation);
        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();

        // act
        using var store = CreateStore(plan, GetShape(plan), payload, resultArena, sourceArena);

        // assert
        RenderData(store).MatchInlineSnapshot(
            """
            {"item":{"a":"first","b":5,"c":{"x":"c","y":1},"d":[{"x":"d1","y":2},{"x":"d2","y":3}],"e":"last"}}
            """);
        Assert.Null(store.Errors);
    }

    [Theory]
    [InlineData(Operation, """{"data":{"item":{"a":null,"b":5,"c":null,"d":[null,{"x":"d2","y":null}],"e":"last"}}}""")]
    [InlineData(Operation, """{"data":{"item":{"a":"first","b":5,"c":{"x":"c"},"d":[],"e":"last"}}}""")]
    [InlineData(Operation, """{"data":{"item":{"a":"first","b":5}}}""")]
    [InlineData(Operation, """{"data":{"item":null}}""")]
    [InlineData(
        Operation,
        """{"data":{"item":{"a":"first","b":5,"c":{"y":1,"x":"c","x":"again"},"d":[{"x":"d1","y":2}],"e":"last"}}}""")]
    [InlineData(
        Operation,
        """{"errors":[{"message":"boom","path":["item","c","x"]}],"data":{"item":{"a":"first","b":5,"c":{"x":null,"y":1},"d":[],"e":"last"}}}""")]
    [InlineData(
        Operation,
        """{"data":{"item":{"a":"first","b":5,"c":null,"d":[{"x":"d1","y":2}],"e":"last"}},"errors":[{"message":"boom","path":["item","c"]},{"message":"root"}]}""")]
    [InlineData(
        "{ pet { name ... on Dog { barks } ... on Cat { lives } } }",
        """{"data":{"pet":{"__typename":"Dog","name":"Rex","barks":true}}}""")]
    [InlineData(
        "{ pet { name ... on Dog { barks } ... on Cat { lives } } }",
        """{"data":{"pet":{"name":"Tom","lives":9,"__typename":"Cat"}}}""")]
    [InlineData(
        "{ pet { name } }",
        """{"errors":[{"message":"boom","path":["pet","name"]}],"data":{"pet":{"__typename":"Cat","name":null}}}""")]
    public void AddPartialResults_Should_CreateTheSameResult_When_TheResultIsParsedWithAndWithoutItsExpectedShape(
        string operation,
        string payload)
    {
        // arrange
        var plan = PlanOperation(s_schema, operation);
        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var expectedResultArena = new MemoryArena();
        using var expectedSourceArena = new MemoryArena();
        using var expected = CreateStore(plan, shape: null, payload, expectedResultArena, expectedSourceArena);

        // act
        using var store = CreateStore(plan, GetShape(plan), payload, resultArena, sourceArena);

        // assert
        Assert.Equal(RenderData(expected), RenderData(store));
        Assert.Equal(
            expected.Errors?.Select(t => $"{t.Message} {t.Path}").ToArray(),
            store.Errors?.Select(t => $"{t.Message} {t.Path}").ToArray());
    }

    [Fact]
    public void AddPartialResults_Should_SkipUnselectedSourceFields_When_TheResultHasItsExpectedShape()
    {
        // arrange
        // the source schema operation selects more fields than the operation of the client
        var plan = PlanOperation(s_schema, "{ item { e c { y } } }");
        var shape = OperationResultShape.Create(ParseOperation(Operation)).Single;
        const string payload =
            """
            {"data":{"item":{"a":"first","b":5,"c":{"x":"c","y":1},"d":[{"x":"d1","y":2}],"e":"last"}}}
            """;
        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();

        // act
        using var store = CreateStore(plan, shape, payload, resultArena, sourceArena);

        // assert
        RenderData(store).MatchInlineSnapshot(
            """
            {"item":{"e":"last","c":{"y":1}}}
            """);
        Assert.Null(store.Errors);
    }

    [Fact]
    public void AddPartialResults_Should_CompleteByName_When_TheShapeKeepsSelectionsForEightOtherSelectionSets()
    {
        // arrange
        const string payload =
            """
            {"data":{"item":{"a":"first","b":5,"c":{"x":"c","y":1},"d":[{"x":"d1","y":2}],"e":"last"}}}
            """;
        var shape = OperationResultShape.Create(ParseOperation(Operation)).Single;
        var completed = new List<string>();

        // act
        // every plan has its own selection sets, the ninth plan finds the lists of the shape full
        for (var i = 0; i < 9; i++)
        {
            using var resultArena = new MemoryArena();
            using var sourceArena = new MemoryArena();
            using var store = CreateStore(PlanOperation(s_schema, Operation), shape, payload, resultArena, sourceArena);

            completed.Add(RenderData(store));
        }

        // assert
        Assert.Equal(
            Enumerable
                .Repeat("""{"item":{"a":"first","b":5,"c":{"x":"c","y":1},"d":[{"x":"d1","y":2}],"e":"last"}}""", 9)
                .ToArray(),
            completed.ToArray());
    }

    /// <summary>
    /// Gets the expected shape of the results of the single source schema operation of
    /// <paramref name="plan"/>.
    /// </summary>
    private static SourceResultShape GetShape(OperationPlan plan)
    {
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));

        return OperationResultShape.Create(
            Utf8GraphQLOperationParser.Parse(node.Operation.Value, ParserOptions.Trusted)).Single;
    }

    private static Utf8OperationDocument ParseOperation(string operation)
        => Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(operation), ParserOptions.Trusted);

    /// <summary>
    /// Creates a store for <paramref name="plan"/> and adds the result <paramref name="payloadJson"/>,
    /// parsed with <paramref name="shape"/> when one is given.
    /// </summary>
    private static FetchResultStore CreateStore(
        OperationPlan plan,
        SourceResultShape? shape,
        string payloadJson,
        MemoryArena resultArena,
        MemoryArena sourceArena)
    {
        var node = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var store = new FetchResultStore();
        store.Initialize(
            resultArena,
            s_schema,
            DefaultErrorHandler.Default,
            plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: default,
            deferFlags: default,
            pathSegmentLocalPoolCapacity: 16);

        using var writer = new ArenaBufferWriter(sourceArena);
        var payload = Encoding.UTF8.GetBytes(payloadJson.Trim());

        payload.CopyTo(writer.GetSpan(payload.Length));
        writer.Advance(payload.Length);

        var document = SourceResultDocument.ParseFilled(
            sourceArena,
            writer.Segments,
            writer.UsedChunks,
            writer.LastLength,
            shape);
        var result = new SourceSchemaResult(CompactPath.Root, document);

        store.AddPartialResults(
            SelectionPath.Root,
            [result],
            node.ResultSelectionSet,
            containsErrors: result.HasErrors);

        return store;
    }

    private static string RenderData(FetchResultStore store)
    {
        using var buffer = new PooledArrayWriter();
        store.Result.Data.WriteTo(buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}

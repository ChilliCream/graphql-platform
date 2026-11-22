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
        { "data": { "item": { "a": "first", "b": 5, "c": { "x": "c", "y": 1 }, "d": [ { "x": "d1", "y": 2 }, { "x": "d2", "y": 3 } ], "e": "last" } } }
        """)]
    public void AddPartialResults_Should_CompleteEveryField_When_TheResultIsParsedWithItsExpectedShape(string payload)
    {
        // arrange
        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();

        // act
        using var store = CreateStore(PlanOperation(s_schema, Operation), Operation, payload, resultArena, sourceArena);

        // assert
        Assert.Equal(
            """
            {"item":{"a":"first","b":5,"c":{"x":"c","y":1},"d":[{"x":"d1","y":2},{"x":"d2","y":3}],"e":"last"}}
            """,
            RenderData(store));
        Assert.Null(store.Errors);
    }

    [Theory]
    [InlineData("""{"data":{"item":{"a":null,"b":5,"c":null,"d":[null,{"x":"d2","y":null}],"e":"last"}}}""")]
    [InlineData("""{"data":{"item":{"a":"first","b":5,"c":{"x":"c"},"d":[],"e":"last"}}}""")]
    [InlineData("""{"data":{"item":{"a":"first","b":5}}}""")]
    [InlineData("""{"data":{"item":null}}""")]
    [InlineData("""{"data":{"item":{"a":"first","b":5,"c":{"y":1,"x":"c","x":"again"},"d":[{"x":"d1","y":2}],"e":"last"}}}""")]
    public void AddPartialResults_Should_CreateTheSameData_When_TheResultIsParsedWithAndWithoutItsExpectedShape(
        string payload)
    {
        // arrange
        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();
        using var expectedResultArena = new MemoryArena();
        using var expectedSourceArena = new MemoryArena();
        var plan = PlanOperation(s_schema, Operation);
        using var expected = CreateStore(plan, null, payload, expectedResultArena, expectedSourceArena);

        // act
        using var store = CreateStore(plan, Operation, payload, resultArena, sourceArena);

        // assert
        Assert.Equal(RenderData(expected), RenderData(store));
        Assert.Equal(expected.Errors?.Count, store.Errors?.Count);
    }

    [Fact]
    public void AddPartialResults_Should_SkipUnselectedSourceFields_When_TheResultHasItsExpectedShape()
    {
        // arrange
        // the source schema operation selects more fields than the operation of the client
        var plan = PlanOperation(s_schema, "{ item { e c { y } } }");
        const string payload =
            """
            {"data":{"item":{"a":"first","b":5,"c":{"x":"c","y":1},"d":[{"x":"d1","y":2}],"e":"last"}}}
            """;
        using var resultArena = new MemoryArena();
        using var sourceArena = new MemoryArena();

        // act
        using var store = CreateStore(plan, Operation, payload, resultArena, sourceArena);

        // assert
        Assert.Equal("""{"item":{"e":"last","c":{"y":1}}}""", RenderData(store));
        Assert.Null(store.Errors);
    }

    /// <summary>
    /// Creates a store for <paramref name="plan"/> and adds the result <paramref name="payloadJson"/>,
    /// parsed with the expected shape of <paramref name="sourceOperation"/> when one is given.
    /// </summary>
    private static FetchResultStore CreateStore(
        OperationPlan plan,
        string? sourceOperation,
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

        var shape = sourceOperation is null
            ? null
            : OperationResultShape.Create(Encoding.UTF8.GetBytes(sourceOperation)).Single;
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
        var added = store.AddPartialResults(
            SelectionPath.Root,
            [new SourceSchemaResult(CompactPath.Root, document)],
            node.ResultSelectionSet,
            containsErrors: false);
        Assert.True(added);

        return store;
    }

    private static string RenderData(FetchResultStore store)
    {
        using var buffer = new PooledArrayWriter();
        store.Result.Data.WriteTo(buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}

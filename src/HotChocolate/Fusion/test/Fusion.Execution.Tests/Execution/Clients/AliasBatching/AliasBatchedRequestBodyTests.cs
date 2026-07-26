using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Fusion.Execution.Clients.AliasBatching.AliasBatchTestData;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

public sealed class AliasBatchedRequestBodyTests
{
    private const string Lookup = "query($__fusion_1_id: ID!){productById(id: $__fusion_1_id){name price}}";

    [Fact]
    public void WriteTo_Should_MergeItemsIntoOneOperation_When_ItemsShareABody()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var items = new[]
        {
            CreateItem(memory, document, """{"__fusion_1_id":"1"}"""),
            CreateItem(memory, document, """{"__fusion_1_id":"2"}""")
        };

        // act
        var json = Write(items, []);

        // assert
        json.MatchInlineSnapshot(
            """
            {"query":"query Batch_0000000000000001($_0___fusion_1_id:ID!,$_1___fusion_1_id:ID!){_0_productById:productById(id:$_0___fusion_1_id){..._fusion_body_1} _1_productById:productById(id:$_1___fusion_1_id){..._fusion_body_1}} fragment _fusion_body_1 on Product{name price}","variables":{"_0___fusion_1_id":"1","_1___fusion_1_id":"2"}}
            """);
    }

    [Fact]
    public void WriteTo_Should_ProduceAParsableOperation_When_ItemsAreMerged()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var items = new[]
        {
            CreateItem(memory, document, """{"__fusion_1_id":"1"}"""),
            CreateItem(memory, document, """{"__fusion_1_id":"2"}""")
        };

        // act
        var query = ReadQuery(Write(items, []));

        // assert
        using var merged = Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(query));
        Assert.Equal(["_0_productById", "_1_productById"], ReadRootAliases(merged));
    }

    [Fact]
    public void WriteTo_Should_DeclareSharedVariableOnce_When_ItemsShareIt()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(
            "query($search: String, $__fusion_1_id: ID!)"
            + "{productById(id: $__fusion_1_id){name(q: $search)}}");
        var shared = GetVariableDefinition(document, "search");
        var items = new[]
        {
            CreateItem(memory, document, """{"search":"abc","__fusion_1_id":"1"}"""),
            CreateItem(memory, document, """{"search":"abc","__fusion_1_id":"2"}""")
        };

        // act
        var query = ReadQuery(Write(items, [shared]));

        // assert
        query.MatchInlineSnapshot(
            "query Batch_0000000000000001($search:String,$_0___fusion_1_id:ID!,"
            + "$_1___fusion_1_id:ID!){_0_productById:productById(id:$_0___fusion_1_id)"
            + "{..._fusion_body_1} _1_productById:productById(id:$_1___fusion_1_id)"
            + "{..._fusion_body_1}} fragment _fusion_body_1 on Product{name(q:$search)}");
    }

    [Fact]
    public void WriteTo_Should_IncludeOnError_When_TheModeIsConfigured()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var items = new[]
        {
            CreateItem(memory, document, """{"__fusion_1_id":"1"}"""),
            CreateItem(memory, document, """{"__fusion_1_id":"2"}""")
        };

        // act
        var json = Write(items, [], ErrorHandlingMode.Null);

        // assert
        Assert.EndsWith("""},"onError":"NULL"}""", json, StringComparison.Ordinal);
    }

    private static string Write(
        AliasBatchItem[] items,
        List<Utf8VariableDefinitionNode> sharedVariables,
        ErrorHandlingMode? onError = null)
    {
        var body = new AliasBatchedRequestBody(items, items.Length, sharedVariables, 1, onError);
        using var buffer = new PooledArrayWriter();
        body.WriteTo(new JsonWriter(buffer, default));
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string ReadQuery(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("query").GetString()!;
    }

    private static List<string> ReadRootAliases(Utf8OperationDocument document)
    {
        var aliases = new List<string>();

        foreach (var operation in document.GetOperations())
        {
            foreach (var selection in operation.SelectionSet.GetSelections())
            {
                aliases.Add(Encoding.UTF8.GetString(selection.GetField().Utf8Alias));
            }
        }

        return aliases;
    }
}

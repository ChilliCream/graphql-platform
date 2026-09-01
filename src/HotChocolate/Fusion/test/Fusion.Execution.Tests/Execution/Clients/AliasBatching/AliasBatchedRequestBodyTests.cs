using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Fusion.Execution.Clients.AliasBatching.AliasBatchTestData;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

public sealed class AliasBatchedRequestBodyTests
{
    private const string Lookup = "query($__fusion_1_id: ID!){productById(id: $__fusion_1_id){name price}}";

    private const string OperationShortHash = "01234567";

    private const ulong CompositionHash = 0x0123456789abcdef;

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
            {"query":"query Op_01234567_Batch_0123456789abcdef($_0___fusion_1_id:ID!,$_1___fusion_1_id:ID!){_0_productById:productById(id:$_0___fusion_1_id){..._fusion_body_1} _1_productById:productById(id:$_1___fusion_1_id){..._fusion_body_1}} fragment _fusion_body_1 on Product{name price}","variables":{"_0___fusion_1_id":"1","_1___fusion_1_id":"2"}}
            """);
    }

    [Fact]
    public void WriteTo_Should_NameTheOperationAfterTheClientOperation_When_ItIsNamed()
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
        var query = ReadQuery(Write(items, [], operationName: "GetProducts"));

        // assert
        Assert.StartsWith(
            "query GetProducts_01234567_Batch_0123456789abcdef(",
            query,
            StringComparison.Ordinal);
    }

    [Fact]
    public void WriteTo_Should_NameTheOperationAfterTheClientOperation_When_ItsNameIsLong()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var items = new[]
        {
            CreateItem(memory, document, """{"__fusion_1_id":"1"}"""),
            CreateItem(memory, document, """{"__fusion_1_id":"2"}""")
        };
        var operationName = new string('a', 300);

        // act
        var query = ReadQuery(Write(items, [], operationName: operationName));

        // assert
        Assert.StartsWith(
            $"query {operationName}_01234567_Batch_0123456789abcdef(",
            query,
            StringComparison.Ordinal);
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
            "query Op_01234567_Batch_0123456789abcdef($search:String,$_0___fusion_1_id:ID!,"
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
        ErrorHandlingMode? onError = null,
        string? operationName = null)
    {
        var body = new AliasBatchedRequestBody(
            items,
            items.Length,
            sharedVariables,
            operationName,
            OperationShortHash,
            CompositionHash,
            onError);
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

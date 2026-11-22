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

    [Fact]
    public void WriteTo_Should_ReplaceTheCharacter_When_TheShortHashHoldsANonNameCharacter()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var items = new[] { CreateItem(memory, document, """{"__fusion_1_id":"1"}""") };

        // act
        var json = Write(items, [], operationShortHash: "0123456-");

        // assert
        Assert.StartsWith(
            "query Op_0123456__Batch_0123456789abcdef(",
            ReadQuery(json),
            StringComparison.Ordinal);
    }

    [Fact]
    public void WriteTo_Should_WriteTheSameBody_When_TheMergedOperationIsCached()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var items = new[]
        {
            CreateItem(memory, document, """{"__fusion_1_id":"1"}"""),
            CreateItem(memory, document, """{"__fusion_1_id":"2"}""")
        };
        var queryCache = new AliasBatchQueryCache();
        var expected = Write(items, []);
        var first = Write(items, [], queryCache: queryCache);
        var second = Write(items, [], queryCache: queryCache);

        // act
        var third = Write(items, [], queryCache: queryCache);

        // assert
        // the cached value is the JSON string token between the query and the variables property.
        const string queryProperty = """{"query":""";
        var queryToken = expected[queryProperty.Length..expected.IndexOf(
            ""","variables":""",
            StringComparison.Ordinal)];
        var key = new AliasBatchQueryKey(items, [], null, OperationShortHash, CompositionHash);
        Assert.True(queryCache.TryGetQuery(in key, out var query));
        Assert.Equal(queryToken, Encoding.UTF8.GetString(query));
        Assert.Equal(expected, first);
        Assert.Equal(expected, second);
        Assert.Equal(expected, third);
    }

    [Fact]
    public void WriteTo_Should_WriteTheVariablesOfTheRequest_When_TheMergedOperationIsCached()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var queryCache = new AliasBatchQueryCache();
        Write(CreateItems("1", "2"), [], queryCache: queryCache);
        Write(CreateItems("1", "2"), [], queryCache: queryCache);
        var items = CreateItems("3", "4");

        // act
        var json = Write(items, [], queryCache: queryCache);

        // assert
        Assert.Equal(Write(items, []), json);

        AliasBatchItem[] CreateItems(string first, string second)
            =>
            [
                CreateItem(memory, document, $$"""{"__fusion_1_id":"{{first}}"}"""),
                CreateItem(memory, document, $$"""{"__fusion_1_id":"{{second}}"}""")
            ];
    }

    [Fact]
    public void WriteTo_Should_BuildTheMergedOperation_When_CachedOperationHasAnotherRootField()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        using var otherDocument = ParseLookup(
            "query($__fusion_1_id: ID!){productById(id: $__fusion_1_id){name}}");
        var cached = new[]
        {
            CreateItem(memory, document, """{"__fusion_1_id":"1"}"""),
            CreateItem(memory, document, """{"__fusion_1_id":"2"}""")
        };
        var items = new[]
        {
            CreateItem(memory, otherDocument, """{"__fusion_1_id":"1"}"""),
            CreateItem(memory, otherDocument, """{"__fusion_1_id":"2"}""")
        };
        var queryCache = new AliasBatchQueryCache();
        Write(cached, [], queryCache: queryCache);
        Write(cached, [], queryCache: queryCache);

        // act
        var json = Write(items, [], queryCache: queryCache);

        // assert
        Assert.Equal(Write(items, []), json);
        Assert.NotEqual(Write(cached, []), json);
    }

    [Fact]
    public void WriteTo_Should_BuildTheMergedOperation_When_CachedOperationHasAnotherItemCount()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var cached = new[]
        {
            CreateItem(memory, document, """{"__fusion_1_id":"1"}"""),
            CreateItem(memory, document, """{"__fusion_1_id":"2"}""")
        };
        var items = new[]
        {
            CreateItem(memory, document, """{"__fusion_1_id":"1"}"""),
            CreateItem(memory, document, """{"__fusion_1_id":"2"}"""),
            CreateItem(memory, document, """{"__fusion_1_id":"3"}""")
        };
        var queryCache = new AliasBatchQueryCache();
        Write(cached, [], queryCache: queryCache);
        Write(cached, [], queryCache: queryCache);

        // act
        var json = Write(items, [], queryCache: queryCache);

        // assert
        Assert.Equal(Write(items, []), json);
    }

    [Fact]
    public void WriteTo_Should_BuildTheMergedOperation_When_CachedOperationHasAnotherLookupType()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var cached = new[] { CreateItem(memory, document, """{"__fusion_1_id":"1"}""") };
        var items = new[]
        {
            CreateItem(memory, document, """{"__fusion_1_id":"1"}""", lookupTypeName: "Book")
        };
        var queryCache = new AliasBatchQueryCache();
        Write(cached, [], queryCache: queryCache);
        Write(cached, [], queryCache: queryCache);

        // act
        var json = Write(items, [], queryCache: queryCache);

        // assert
        Assert.Equal(Write(items, []), json);
    }

    [Fact]
    public void WriteTo_Should_BuildTheMergedOperation_When_CachedOperationHasAnotherName()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var items = new[] { CreateItem(memory, document, """{"__fusion_1_id":"1"}""") };
        var queryCache = new AliasBatchQueryCache();
        Write(items, [], queryCache: queryCache);
        Write(items, [], queryCache: queryCache);

        // act
        var named = Write(items, [], operationName: "GetProducts", queryCache: queryCache);
        var hashed = Write(items, [], operationShortHash: "76543210", queryCache: queryCache);

        // assert
        Assert.Equal(Write(items, [], operationName: "GetProducts"), named);
        Assert.Equal(Write(items, [], operationShortHash: "76543210"), hashed);
    }

    [Fact]
    public void WriteTo_Should_BuildTheMergedOperation_When_CachedOperationHasOtherSharedVariables()
    {
        // arrange
        const string lookup =
            "query($__fusion_1_id: ID!, $first: Int){productById(id: $__fusion_1_id){reviews(first: $first){id}}}";
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(lookup);
        var items = new[]
        {
            CreateItem(memory, document, """{"__fusion_1_id":"1","first":2}"""),
            CreateItem(memory, document, """{"__fusion_1_id":"2","first":2}""")
        };
        var sharedVariables = new List<Utf8VariableDefinitionNode> { GetVariableDefinition(document, "first") };
        var queryCache = new AliasBatchQueryCache();
        Write(items, [], queryCache: queryCache);
        Write(items, [], queryCache: queryCache);

        // act
        var json = Write(items, sharedVariables, queryCache: queryCache);

        // assert
        Assert.Equal(Write(items, sharedVariables), json);
        Assert.NotEqual(Write(items, []), json);
    }

    [Fact]
    public void WriteTo_Should_NotCacheTheMergedOperation_When_ItIsLongerThanTheLimit()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var items = new[] { CreateItem(memory, document, """{"__fusion_1_id":"1"}""") };
        var queryCache = new AliasBatchQueryCache(bucketCount: 1, maxQueryLength: 16);
        Write(items, [], queryCache: queryCache);
        Write(items, [], queryCache: queryCache);

        // act
        var json = Write(items, [], queryCache: queryCache);

        // assert
        var key = new AliasBatchQueryKey(items, [], null, OperationShortHash, CompositionHash);
        Assert.False(queryCache.TryGetQuery(in key, out _));
        Assert.Equal(Write(items, []), json);
    }

    private static string Write(
        AliasBatchItem[] items,
        List<Utf8VariableDefinitionNode> sharedVariables,
        ErrorHandlingMode? onError = null,
        string? operationName = null,
        string operationShortHash = OperationShortHash,
        AliasBatchQueryCache? queryCache = null)
    {
        var body = new AliasBatchedRequestBody(
            items,
            items.Length,
            sharedVariables,
            operationName,
            operationShortHash,
            CompositionHash,
            onError,
            queryCache);
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

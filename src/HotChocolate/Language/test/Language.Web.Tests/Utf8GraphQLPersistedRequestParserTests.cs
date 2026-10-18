using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Language;

public sealed class Utf8GraphQLPersistedRequestParserTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsePersistedOperation_Should_PreserveOverridesAndOwnership_When_BodyHasRequestFields(bool sequence)
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            """{"id":"body-id","operationName":"Body","onError":"NULL","variables":{"n":2},"extensions":{"flag":true}}""");
        var parser = new Utf8GraphQLRequestParser();

        // act
        using var request = sequence
            ? parser.ParsePersistedOperation(new OperationDocumentId("route-id"), "Route", new ReadOnlySequence<byte>(source))
            : parser.ParsePersistedOperation(new OperationDocumentId("route-id"), "Route", source);
        Array.Fill(source, (byte)'x');
        var variables = request.Variables!;
        var extensions = request.Extensions!;
        var live = new
        {
            DocumentId = request.DocumentId?.Value,
            request.OperationName,
            request.ErrorHandlingMode,
            request.HasDocumentBody,
            Variables = variables.RootElement.GetRawText(),
            Extensions = extensions.RootElement.GetRawText(),
            HasVariablesMemoryOwner = request.VariablesMemoryOwner is not null,
            HasExtensionsMemoryOwner = request.ExtensionsMemoryOwner is not null
        };
        request.Dispose();

        // assert
        Snapshot.Create()
            .Add(live, "Live Request After Input Reuse")
            .Add(new
            {
                VariablesException = Record.Exception(() => variables.RootElement.GetProperty("n").GetInt32())?.GetType().Name,
                ExtensionsException = Record.Exception(() => extensions.RootElement.GetProperty("flag").GetBoolean())?.GetType().Name
            }, "Disposed Documents")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ParsePersistedOperation_Should_UseRouteName_When_RouteNameIsNullOrEmpty(string? routeName)
    {
        // arrange
        var parser = new Utf8GraphQLRequestParser();

        // act
        using var request = parser.ParsePersistedOperation(
            new OperationDocumentId("route-id"), routeName, """{"operationName":"Body"}"""u8);

        // assert
        Assert.Equal(routeName, request.OperationName);
        Assert.Equal("route-id", request.DocumentId?.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsePersistedOperation_Should_UseBodyIdForCache_When_RouteOverridesBodyId(bool cacheHit)
    {
        // arrange
        var events = new List<string>();
        var cache = new RecordingCache(events);
        var cachedDocument = Utf8GraphQLParser.Parse("query Cached { cached }");
        if (cacheHit)
        {
            cache.TryAddDocument("body-id", new CachedDocument(
                cachedDocument, new OperationDocumentHash("cachedhash", "sha256", HashFormat.Hex), true));
        }
        var parser = new Utf8GraphQLRequestParser(
            cache: cache, hashProvider: new RecordingHashProvider(events));

        // act
        using var request = parser.ParsePersistedOperation(
            new OperationDocumentId("route-id"),
            "Route",
            """{"id":"body-id","operationName":"Body","query":"query Body { field }"}"""u8);

        // assert
        Snapshot.Create(cacheHit ? "CacheHit" : "CacheMiss")
            .Add(new
            {
                DocumentId = request.DocumentId?.Value,
                request.OperationName,
                Document = request.Document?.ToString(),
                Hash = request.DocumentHash?.Value,
                Algorithm = request.DocumentHash?.AlgorithmName,
                request.HasDocumentBody,
                SameCachedDocument = ReferenceEquals(cachedDocument, request.Document)
            }, "Final Route Request")
            .Add(events, "Cache And Hash Callbacks")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void ParsePersistedOperation_Should_DropSkippedBodyFlag_When_QueryBodyIsSkipped()
    {
        // arrange
        var events = new List<string>();
        var parser = new Utf8GraphQLRequestParser(
            cache: new RecordingCache(events),
            hashProvider: new RecordingHashProvider(events),
            skipDocumentBody: true);

        // act
        using var request = parser.ParsePersistedOperation(
            new OperationDocumentId("route-id"),
            null,
            """{"id":"body-id","operationName":"Body","query":"invalid !!! GraphQL"}"""u8);

        // assert
        Assert.False(request.HasDocumentBody);
        Assert.Null(request.Document);
        Assert.Null(request.OperationName);
        Assert.Equal("route-id", request.DocumentId?.Value);
        Assert.Empty(events);
    }

    [Theory]
    [InlineData("""{"id":42,"query":"{ field }"}""")]
    [InlineData("""{"id":"bad+id","query":"{ field }"}""")]
    [InlineData("""{"operationName":42,"query":"{ field }"}""")]
    public void ParsePersistedOperation_Should_ValidateBodyBeforeOverrides_When_BodyIsInvalid(string body)
    {
        // arrange
        var events = new List<string>();
        var cache = new RecordingCache(events);
        var hashProvider = new RecordingHashProvider(events);
        var source = Encoding.UTF8.GetBytes(body);

        // act
        var exception = Record.Exception(() =>
        {
            var parser = new Utf8GraphQLRequestParser(cache: cache, hashProvider: hashProvider);
            parser.ParsePersistedOperation(new OperationDocumentId("route-id"), null, source);
        });

        // assert
        Assert.IsType<InvalidGraphQLRequestException>(exception);
        Assert.Empty(events);
    }

    [Theory]
    [InlineData("{}", nameof(InvalidGraphQLRequestException))]
    [InlineData("""{"id":"body-id"}""", nameof(ArgumentNullException))]
    public void ParsePersistedOperation_Should_PreserveValidationOrder_When_RouteIdIsEmpty(
        string body,
        string exceptionType)
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(body);

        // act
        var exception = Record.Exception(() =>
        {
            var parser = new Utf8GraphQLRequestParser();
            parser.ParsePersistedOperation(OperationDocumentId.Empty, null, source);
        });

        // assert
        Assert.Equal(exceptionType, exception?.GetType().Name);
        Assert.Equal(
            exceptionType == nameof(ArgumentNullException) ? "document" : null,
            (exception as ArgumentNullException)?.ParamName);
    }

    [Fact]
    public void ParsePersistedOperation_Should_RetainEmptyRouteId_When_BodyContainsDocument()
    {
        // arrange
        var parser = new Utf8GraphQLRequestParser();

        // act
        using var request = parser.ParsePersistedOperation(
            OperationDocumentId.Empty, null, """{"id":"body-id","query":"{ field }"}"""u8);

        // assert
        Assert.True(request.DocumentId.HasValue);
        Assert.True(request.DocumentId.GetValueOrDefault().IsEmpty);
        Assert.True(request.HasDocumentBody);
    }

    [Fact]
    public void ParsePersistedOperation_Should_PreserveRootHandling_When_BodyIsPrimitive()
    {
        // arrange
        var parser = new Utf8GraphQLRequestParser();

        // act
        using var request = parser.ParsePersistedOperation(new OperationDocumentId("route-id"), null, "1"u8);

        // assert
        Assert.Equal("route-id", request.DocumentId?.Value);
        Assert.False(request.HasDocumentBody);
    }

    [Fact]
    public void ParsePersistedOperation_Should_WrapJsonFailure_When_BodyIsTruncated()
    {
        // arrange
        var source = """{"variables":{"""u8.ToArray();

        // act
        var exception = Record.Exception(() =>
        {
            var parser = new Utf8GraphQLRequestParser();
            parser.ParsePersistedOperation(new OperationDocumentId("route-id"), null, source);
        });

        // assert
        var requestException = Assert.IsType<InvalidGraphQLRequestException>(exception);
        Assert.IsAssignableFrom<JsonException>(requestException.InnerException);
    }

    private sealed class RecordingCache(List<string> events) : IDocumentCache
    {
        private readonly Dictionary<string, CachedDocument> _documents = [];

        public int Capacity => int.MaxValue;

        public int Count => _documents.Count;

        public bool TryGetDocument(string documentId, [NotNullWhen(true)] out CachedDocument? document)
        {
            events.Add("cache:" + documentId);
            return _documents.TryGetValue(documentId, out document);
        }

        public void TryAddDocument(string documentId, CachedDocument document)
            => _documents.Add(documentId, document);
    }

    private sealed class RecordingHashProvider(List<string> events) : IDocumentHashProvider
    {
        private readonly Sha256DocumentHashProvider _inner = new(HashFormat.Hex);

        public string Name => _inner.Name;

        public string AlgorithmName => _inner.AlgorithmName;

        public HashFormat Format => _inner.Format;

        public OperationDocumentHash ComputeHash(ReadOnlySpan<byte> document)
        {
            events.Add("hash:span");
            return _inner.ComputeHash(document);
        }

        public OperationDocumentHash ComputeHash(ReadOnlySequence<byte> document)
        {
            events.Add("hash:sequence");
            return _inner.ComputeHash(document);
        }
    }
}

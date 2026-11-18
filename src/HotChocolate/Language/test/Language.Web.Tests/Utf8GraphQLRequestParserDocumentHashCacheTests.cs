using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace HotChocolate.Language;

public class Utf8GraphQLRequestParserDocumentHashCacheTests
{
    private const string Request = """{ "query": "query GetA {\n  a\n}" }""";
    private const string OtherRequest = """{ "query": "query GetB {\n  b\n}" }""";

    [Fact]
    public void Parse_Should_NotComputeHash_When_DocumentIsRepeated()
    {
        // arrange
        var context = new ParserContext();
        var remembered = context.ParseUntilRemembered(Request);
        var computed = context.HashProvider.Computed;

        // act
        var result = context.Parse(Request);

        // assert
        Assert.Equal(computed, context.HashProvider.Computed);
        Assert.Same(remembered.Document, result.Document);
        Assert.Equal(remembered.DocumentHash, result.DocumentHash);
        Assert.Equal(remembered.DocumentId, result.DocumentId);
        Assert.Equal(context.ComputeHash("query GetA {\n  a\n}"), result.DocumentHash);
    }

    [Fact]
    public void Parse_Should_ComputeHash_When_DocumentIsSeenForTheSecondTime()
    {
        // arrange
        var context = new ParserContext();
        context.ParseAndCache(Request);

        // act
        context.Parse(Request);

        // assert
        Assert.Equal(2, context.HashProvider.Computed);
    }

    [Fact]
    public void Parse_Should_ReturnHashOfDocument_When_OtherDocumentIsRemembered()
    {
        // arrange
        var context = new ParserContext();
        context.ParseUntilRemembered(Request);

        // act
        var result = context.Parse(OtherRequest);

        // assert
        Assert.Equal(context.ComputeHash("query GetB {\n  b\n}"), result.DocumentHash);
        Assert.Equal("query GetB { b }", result.Document!.ToString(indented: false));
    }

    [Theory]
    [InlineData("""{ "id": "client-id", "query": "query GetA {\n  a\n}" }""")]
    [InlineData("""{ "query": "query GetA {\n  a\n}", "documentId": "client-id" }""")]
    public void Parse_Should_KeepDocumentId_When_IdIsNotCachedAndDocumentIsRemembered(string requestWithId)
    {
        // arrange
        var context = new ParserContext();
        var remembered = context.ParseUntilRemembered(Request);
        var computed = context.HashProvider.Computed;

        // act
        var result = context.Parse(requestWithId);

        // assert
        Assert.Equal("client-id", result.DocumentId?.Value);
        Assert.Equal(remembered.DocumentHash, result.DocumentHash);
        Assert.Same(remembered.Document, result.Document);
        Assert.Equal(computed, context.HashProvider.Computed);
        Assert.Equal(
            new[] { "client-id", remembered.DocumentHash!.Value.Value },
            context.DocumentCache.Lookups.TakeLast(2).ToArray());
    }

    [Fact]
    public void Parse_Should_ReturnDocumentOfId_When_IdIsCachedAndDocumentIsRemembered()
    {
        // arrange
        var context = new ParserContext();
        context.ParseUntilRemembered(Request);
        var documentOfId = Utf8GraphQLParser.Parse("{ c }");
        var hashOfId = new OperationDocumentHash("hash-of-id", "custom", HashFormat.Hex);
        context.DocumentCache.TryAddDocument("client-id", new CachedDocument(documentOfId, hashOfId, false));

        // act
        var result = context.Parse("""{ "id": "client-id", "query": "query GetA {\n  a\n}" }""");

        // assert
        Assert.Equal("client-id", result.DocumentId?.Value);
        Assert.Same(documentOfId, result.Document);
        Assert.Equal(hashOfId, result.DocumentHash);
        Assert.Equal("client-id", context.DocumentCache.Lookups[^1]);
    }

    [Fact]
    public void Parse_Should_ParseDocument_When_RememberedDocumentIsNoLongerCached()
    {
        // arrange
        var context = new ParserContext();
        var remembered = context.ParseUntilRemembered(Request);
        var computed = context.HashProvider.Computed;
        context.DocumentCache.Clear();

        // act
        var result = context.Parse(Request);

        // assert
        Assert.NotSame(remembered.Document, result.Document);
        Assert.Equal("query GetA { a }", result.Document!.ToString(indented: false));
        Assert.Equal(remembered.DocumentHash, result.DocumentHash);
        Assert.Equal(remembered.DocumentId, result.DocumentId);
        Assert.Equal(computed + 1, context.HashProvider.Computed);
    }

    [Fact]
    public void Parse_Should_NotComputeHash_When_RepeatedDocumentSpansSegments()
    {
        // arrange
        var context = new ParserContext();
        var remembered = context.ParseUntilRemembered(Request);
        var computed = context.HashProvider.Computed;
        var segments = SequenceHelper.CreateMultiSegment(Encoding.UTF8.GetBytes(Request));

        // act
        var result = context.Parse(segments);

        // assert
        Assert.Equal(computed, context.HashProvider.Computed);
        Assert.Same(remembered.Document, result.Document);
        Assert.Equal(remembered.DocumentHash, result.DocumentHash);
    }

    [Fact]
    public void Parse_Should_ReturnSameHash_When_DocumentsDifferOnlyInEscaping()
    {
        // arrange
        const string escapedRequest = """{ "query": "{ a }" }""";
        const string plainRequest = """{ "query": "{ a }" }""";
        var context = new ParserContext();
        var escaped = context.ParseUntilRemembered(escapedRequest);
        var plain = context.ParseUntilRemembered(plainRequest);
        var computed = context.HashProvider.Computed;

        // act
        var escapedResult = context.Parse(escapedRequest);
        var plainResult = context.Parse(plainRequest);

        // assert
        Assert.Equal(computed, context.HashProvider.Computed);
        Assert.Equal(context.ComputeHash("{ a }"), escapedResult.DocumentHash);
        Assert.Equal(context.ComputeHash("{ a }"), plainResult.DocumentHash);
        Assert.Same(escaped.Document, escapedResult.Document);
        Assert.Same(plain.Document, plainResult.Document);
    }

    [Fact]
    public void Parse_Should_ComputeHash_When_DocumentIsLongerThanTheLimit()
    {
        // arrange
        var context = new ParserContext(maxDocumentLength: 8);
        context.ParseUntilRemembered(Request);
        var computed = context.HashProvider.Computed;

        // act
        context.Parse(Request);

        // assert
        Assert.Equal(computed + 1, context.HashProvider.Computed);
    }

    [Fact]
    public void Parse_Should_ComputeHash_When_HashCacheBelongsToAnotherHashProvider()
    {
        // arrange
        var context = new ParserContext(hashCacheProvider: new MD5DocumentHashProvider(HashFormat.Hex));
        context.ParseUntilRemembered(Request);
        var computed = context.HashProvider.Computed;

        // act
        context.Parse(Request);

        // assert
        Assert.Equal(computed + 1, context.HashProvider.Computed);
    }

    [Fact]
    public void Add_Should_NotStoreHash_When_DocumentMissedOnce()
    {
        // arrange
        var cache = new DocumentHashCache(new MD5DocumentHashProvider(HashFormat.Hex));
        var document = "{ a }"u8;
        var key = DocumentHashCache.ComputeKey(document);

        // act
        cache.Add(document, key, new OperationDocumentHash("a", "custom", HashFormat.Hex));

        // assert
        Assert.False(cache.TryGetHash(document, key, out _));
    }

    [Fact]
    public void Add_Should_StoreHash_When_DocumentMissedTwice()
    {
        // arrange
        var cache = new DocumentHashCache(new MD5DocumentHashProvider(HashFormat.Hex));
        var document = "{ a }"u8;
        var key = DocumentHashCache.ComputeKey(document);
        var hash = new OperationDocumentHash("a", "custom", HashFormat.Hex);
        cache.Add(document, key, hash);

        // act
        cache.Add(document, key, hash);

        // assert
        Assert.True(cache.TryGetHash(document, key, out var stored));
        Assert.Equal(hash, stored);
    }

    [Fact]
    public void TryGetHash_Should_ReturnFalse_When_KeyMatchesAndBytesDiffer()
    {
        // arrange
        var cache = new DocumentHashCache(new MD5DocumentHashProvider(HashFormat.Hex));
        var hash = new OperationDocumentHash("a", "custom", HashFormat.Hex);
        cache.Add("{ a }"u8, 42, hash);
        cache.Add("{ a }"u8, 42, hash);

        // act
        var found = cache.TryGetHash("{ b }"u8, 42, out _);

        // assert
        Assert.False(found);
        Assert.True(cache.TryGetHash("{ a }"u8, 42, out _));
    }

    [Fact]
    public void Add_Should_ReplaceEntriesInTurn_When_BucketIsFull()
    {
        // arrange
        var cache = new DocumentHashCache(
            new MD5DocumentHashProvider(HashFormat.Hex),
            bucketCount: 1,
            maxDocumentLength: 64);
        var documents = Enumerable.Range(1, 6).Select(i => Encoding.UTF8.GetBytes($"{{ field{i} }}")).ToArray();

        // act
        for (var i = 0; i < documents.Length; i++)
        {
            var hash = new OperationDocumentHash($"hash{i}", "custom", HashFormat.Hex);
            cache.Add(documents[i], i + 1, hash);
            cache.Add(documents[i], i + 1, hash);
        }

        // assert
        var stored = documents.Select((document, i) => cache.TryGetHash(document, i + 1, out _)).ToArray();
        Assert.Equal(new[] { true, false, false, true, true, true }, stored);
    }

    private sealed class ParserContext(int maxDocumentLength = 1024, IDocumentHashProvider? hashCacheProvider = null)
    {
        private DocumentHashCache? _hashCache;

        public TestDocumentCache DocumentCache { get; } = new();

        public CountingHashProvider HashProvider { get; } = new();

        private DocumentHashCache HashCache
            => _hashCache ??= new DocumentHashCache(
                hashCacheProvider ?? HashProvider,
                bucketCount: 64,
                maxDocumentLength);

        public GraphQLRequest Parse(string request)
            => CreateParser().Parse(Encoding.UTF8.GetBytes(request))[0];

        public GraphQLRequest Parse(ReadOnlySequence<byte> request)
            => CreateParser().Parse(request)[0];

        /// <summary>
        /// Parses the request and caches its document the way the request pipeline does.
        /// </summary>
        public GraphQLRequest ParseAndCache(string request)
        {
            var result = Parse(request);

            DocumentCache.TryAddDocument(
                result.DocumentId!.Value.Value,
                new CachedDocument(result.Document!, result.DocumentHash!.Value, false));

            return result;
        }

        /// <summary>
        /// Parses the request until the hash of its document is stored in the hash cache.
        /// </summary>
        public GraphQLRequest ParseUntilRemembered(string request)
        {
            ParseAndCache(request);
            Parse(request);
            return Parse(request);
        }

        public OperationDocumentHash ComputeHash(string document)
            => new MD5DocumentHashProvider(HashFormat.Hex).ComputeHash(Encoding.UTF8.GetBytes(document));

        private Utf8GraphQLRequestParser CreateParser()
            => new(ParserOptions.Default, DocumentCache, HashProvider, skipDocumentBody: false, HashCache);
    }

    private sealed class CountingHashProvider : IDocumentHashProvider
    {
        private readonly MD5DocumentHashProvider _inner = new(HashFormat.Hex);

        public int Computed { get; private set; }

        public string Name => _inner.Name;

        public string AlgorithmName => _inner.AlgorithmName;

        public HashFormat Format => _inner.Format;

        public OperationDocumentHash ComputeHash(ReadOnlySpan<byte> document)
        {
            Computed++;
            return _inner.ComputeHash(document);
        }

        public OperationDocumentHash ComputeHash(ReadOnlySequence<byte> document)
        {
            Computed++;
            return _inner.ComputeHash(document);
        }
    }

    private sealed class TestDocumentCache : IDocumentCache
    {
        private readonly Dictionary<string, CachedDocument> _cache = [];

        public int Capacity => int.MaxValue;

        public int Count => _cache.Count;

        public List<string> Lookups { get; } = [];

        public void Clear() => _cache.Clear();

        public void TryAddDocument(string documentId, CachedDocument document)
            => _cache.TryAdd(documentId, document);

        public bool TryGetDocument(string documentId, [NotNullWhen(true)] out CachedDocument? document)
        {
            Lookups.Add(documentId);
            return _cache.TryGetValue(documentId, out document);
        }
    }
}

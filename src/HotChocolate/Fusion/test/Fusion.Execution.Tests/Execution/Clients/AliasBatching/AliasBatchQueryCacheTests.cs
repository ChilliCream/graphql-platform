using HotChocolate.Buffers;
using HotChocolate.Language;
using static HotChocolate.Fusion.Execution.Clients.AliasBatching.AliasBatchTestData;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

public sealed class AliasBatchQueryCacheTests
{
    private const string ProductLookup =
        "query($__fusion_1_id: ID!){productById(id: $__fusion_1_id){name}}";

    private const string UserLookup =
        "query($__fusion_2_id: ID!){userById(id: $__fusion_2_id){name}}";

    private const string ShortHash = "01234567";

    [Fact]
    public void TryGetQuery_Should_ReturnFalse_When_NoCompositionWasAdded()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(ProductLookup);
        var items = CreateItems(memory, document, document);
        var cache = new AliasBatchQueryCache();

        // act
        var found = cache.TryGetQuery("Op", ShortHash, items, out var query);

        // assert
        Assert.False(found);
        Assert.True(query.IsEmpty);
    }

    [Fact]
    public void TryGetQuery_Should_ReturnTheAddedQuery_When_TheCompositionMatches()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var products = ParseLookup(ProductLookup);
        using var users = ParseLookup(UserLookup);
        var cache = new AliasBatchQueryCache();
        cache.Add("Op", ShortHash, CreateItems(memory, products, users), [1, 2, 3]);

        // act
        var found = cache.TryGetQuery("Op", ShortHash, CreateItems(memory, products, users), out var query);

        // assert
        Assert.True(found);
        Assert.Equal([1, 2, 3], query.ToArray());
    }

    [Fact]
    public void TryGetQuery_Should_ReturnFalse_When_TheItemOrderDiffers()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var products = ParseLookup(ProductLookup);
        using var users = ParseLookup(UserLookup);
        var cache = new AliasBatchQueryCache();
        cache.Add("Op", ShortHash, CreateItems(memory, products, users), [1]);

        // act
        var found = cache.TryGetQuery("Op", ShortHash, CreateItems(memory, users, products), out _);

        // assert
        Assert.False(found);
    }

    [Fact]
    public void TryGetQuery_Should_ReturnFalse_When_TheItemCountDiffers()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var products = ParseLookup(ProductLookup);
        var cache = new AliasBatchQueryCache();
        cache.Add("Op", ShortHash, CreateItems(memory, products, products), [1]);

        // act
        var found = cache.TryGetQuery(
            "Op",
            ShortHash,
            CreateItems(memory, products, products, products),
            out _);

        // assert
        Assert.False(found);
    }

    [Fact]
    public void TryGetQuery_Should_ReturnFalse_When_AnItemComesFromAnotherDocument()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var products = ParseLookup(ProductLookup);
        using var sameText = ParseLookup(ProductLookup);
        var cache = new AliasBatchQueryCache();
        cache.Add("Op", ShortHash, CreateItems(memory, products, products), [1]);

        // act
        var found = cache.TryGetQuery("Op", ShortHash, CreateItems(memory, products, sameText), out _);

        // assert
        Assert.False(found);
    }

    [Theory]
    [InlineData("Other", ShortHash)]
    [InlineData(null, ShortHash)]
    [InlineData("Op", "76543210")]
    public void TryGetQuery_Should_ReturnFalse_When_TheClientOperationDiffers(
        string? operationName,
        string operationShortHash)
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var products = ParseLookup(ProductLookup);
        var cache = new AliasBatchQueryCache();
        cache.Add("Op", ShortHash, CreateItems(memory, products, products), [1]);

        // act
        var found = cache.TryGetQuery(
            operationName,
            operationShortHash,
            CreateItems(memory, products, products),
            out _);

        // assert
        Assert.False(found);
    }

    [Fact]
    public void TryGetQuery_Should_ReturnFalse_When_TheCompositionWasEvicted()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var products = ParseLookup(ProductLookup);
        var cache = new AliasBatchQueryCache();

        // each composition differs in its item count
        for (var itemCount = 2; itemCount <= 6; itemCount++)
        {
            cache.Add("Op", ShortHash, CreateItems(memory, products, itemCount), [(byte)itemCount]);
        }

        // act
        var evicted = cache.TryGetQuery("Op", ShortHash, CreateItems(memory, products, 2), out _);
        var kept = cache.TryGetQuery("Op", ShortHash, CreateItems(memory, products, 3), out var query);

        // assert
        Assert.False(evicted);
        Assert.True(kept);
        Assert.Equal([3], query.ToArray());
    }

    [Fact]
    public void ShouldAdd_Should_ReturnTrue_When_TheCompositionRepeatsTheLastMiss()
    {
        // arrange
        var cache = new AliasBatchQueryCache();

        // act
        var first = cache.ShouldAdd(compositionHash: 1, itemCount: 2);
        var other = cache.ShouldAdd(compositionHash: 2, itemCount: 2);
        var repeated = cache.ShouldAdd(compositionHash: 2, itemCount: 2);

        // assert
        Assert.Equal([false, false, true], [first, other, repeated]);
    }

    [Fact]
    public void ShouldAdd_Should_ReturnFalse_When_TheCompositionHoldsTooManyItems()
    {
        // arrange
        var cache = new AliasBatchQueryCache();
        const int itemCount = AliasBatchQueryCache.MaxItemCount + 1;
        cache.ShouldAdd(compositionHash: 1, itemCount);

        // act
        var repeated = cache.ShouldAdd(compositionHash: 1, itemCount);

        // assert
        Assert.False(repeated);
    }

    private static AliasBatchItem[] CreateItems(
        ChunkedArrayWriter memory,
        params Utf8OperationDocument[] documents)
    {
        var items = new AliasBatchItem[documents.Length];

        for (var i = 0; i < documents.Length; i++)
        {
            items[i] = CreateItem(memory, documents[i], "{}", requestIndex: i);
        }

        return items;
    }

    private static AliasBatchItem[] CreateItems(
        ChunkedArrayWriter memory,
        Utf8OperationDocument document,
        int itemCount)
    {
        var documents = new Utf8OperationDocument[itemCount];
        Array.Fill(documents, document);
        return CreateItems(memory, documents);
    }
}

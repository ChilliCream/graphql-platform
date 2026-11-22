using HotChocolate.Buffers;
using static HotChocolate.Fusion.Execution.Clients.AliasBatching.AliasBatchTestData;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

public sealed class AliasBatchQueryCacheTests
{
    private const string Lookup = "query($__fusion_1_id: ID!){productById(id: $__fusion_1_id){name price}}";

    [Fact]
    public void ShouldStore_Should_ReturnTrue_When_TheSameCompositionMissedBefore()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var items = new[] { CreateItem(memory, document, """{"__fusion_1_id":"1"}""") };
        var cache = new AliasBatchQueryCache();
        var key = new AliasBatchQueryKey(items, [], null, "01234567", 42);

        // act
        var first = cache.ShouldStore(in key);
        var second = cache.ShouldStore(in key);

        // assert
        Assert.False(first);
        Assert.True(second);
    }

    [Fact]
    public void Add_Should_KeepAsManyEntriesAsTheCapacity_When_MoreCompositionsAreStored()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var items = new[] { CreateItem(memory, document, """{"__fusion_1_id":"1"}""") };
        var cache = new AliasBatchQueryCache(capacity: 4, maxQueryLength: 64);

        // act
        for (var i = 1; i <= 6; i++)
        {
            cache.Add(new AliasBatchQueryKey(items, [], null, "01234567", (ulong)i), [(byte)i]);
        }

        // assert
        var stored = 0;

        for (var i = 1; i <= 6; i++)
        {
            if (cache.TryGetQuery(new AliasBatchQueryKey(items, [], null, "01234567", (ulong)i), out _))
            {
                stored++;
            }
        }

        Assert.Equal(4, stored);
        Assert.True(cache.TryGetQuery(new AliasBatchQueryKey(items, [], null, "01234567", 6), out _));
    }

    [Fact]
    public void TryGetQuery_Should_ReturnFalse_When_AnotherCompositionHasTheSameHash()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var items = new[] { CreateItem(memory, document, """{"__fusion_1_id":"1"}""") };
        var cache = new AliasBatchQueryCache();
        cache.Add(new AliasBatchQueryKey(items, [], "GetProducts", "01234567", 42), "query"u8);

        // act
        var found = cache.TryGetQuery(new AliasBatchQueryKey(items, [], "GetReviews", "01234567", 42), out _);

        // assert
        Assert.False(found);
    }

    [Fact]
    public void Add_Should_NotStoreQuery_When_QueryIsLongerThanTheMaximum()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var items = new[] { CreateItem(memory, document, """{"__fusion_1_id":"1"}""") };
        var cache = new AliasBatchQueryCache(capacity: 4, maxQueryLength: 4);
        var key = new AliasBatchQueryKey(items, [], null, "01234567", 42);

        // act
        cache.Add(in key, "query"u8);

        // assert
        Assert.False(cache.TryGetQuery(in key, out _));
    }

    [Fact]
    public void TryGetQuery_Should_ReturnTheStoredQuery_When_TheInputsMatch()
    {
        // arrange
        using var memory = new ChunkedArrayWriter();
        using var document = ParseLookup(Lookup);
        var items = new[] { CreateItem(memory, document, """{"__fusion_1_id":"1"}""") };
        var cache = new AliasBatchQueryCache();
        var key = new AliasBatchQueryKey(items, [], "GetProducts", "01234567", 42);
        cache.Add(in key, "query"u8);

        // act
        var found = cache.TryGetQuery(in key, out var query);

        // assert
        Assert.True(found);
        Assert.Equal("query"u8.ToArray(), query.ToArray());
    }
}

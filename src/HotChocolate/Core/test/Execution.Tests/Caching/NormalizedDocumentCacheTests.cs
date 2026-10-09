using HotChocolate.Execution.Caching;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public sealed class NormalizedDocumentCacheTests
{
    [Fact]
    public void TryGet_Returns_False_When_Entry_Is_Missing()
    {
        // arrange
        var cache = new NormalizedDocumentCache();

        // act
        var found = cache.TryGet("op1", out var document);

        // assert
        Assert.False(found);
        Assert.Null(document);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void TryAdd_Then_TryGet_Returns_The_Same_Document()
    {
        // arrange
        var cache = new NormalizedDocumentCache();
        var document = Utf8GraphQLParser.Parse("{ foo }");

        // act
        cache.TryAdd("op1", document);
        var found = cache.TryGet("op1", out var cachedDocument);

        // assert
        Assert.True(found);
        Assert.Same(document, cachedDocument);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void TryAdd_Does_Not_Overwrite_An_Existing_Entry()
    {
        // arrange
        var cache = new NormalizedDocumentCache();
        var firstDocument = Utf8GraphQLParser.Parse("{ foo }");
        var secondDocument = Utf8GraphQLParser.Parse("{ bar }");

        // act
        cache.TryAdd("op1", firstDocument);
        cache.TryAdd("op1", secondDocument);
        cache.TryGet("op1", out var cachedDocument);

        // assert
        Assert.Same(firstDocument, cachedDocument);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Capacity_Reflects_The_Constructor_Argument()
    {
        // arrange & act
        var cache = new NormalizedDocumentCache(42);

        // assert
        Assert.Equal(42, cache.Capacity);
    }

    [Fact]
    public async Task Schema_Services_Expose_A_Cache_Sized_Like_The_Prepared_Operation_Cache()
    {
        // arrange
        const int cacheCapacity = 517;
        var services = new ServiceCollection();
        services
            .AddGraphQL()
            .ModifyOptions(o => o.PreparedOperationCacheSize = cacheCapacity)
            .AddQueryType(d => d.Field("foo").Resolve(""));
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var normalizedDocumentCache = executor.Schema.Services.GetRequiredService<NormalizedDocumentCache>();

        // assert
        Assert.Equal(cacheCapacity, normalizedDocumentCache.Capacity);
    }
}

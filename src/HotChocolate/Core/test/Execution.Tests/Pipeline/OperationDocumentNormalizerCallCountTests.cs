using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

public sealed class OperationDocumentNormalizerCallCountTests
{
    [Fact]
    public async Task Operation_Cache_Hit_Skips_The_Normalizer_And_A_Miss_Calls_It_Exactly_Once()
    {
        // arrange
        var normalizeCallCount = 0;

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .UseDefaultPipeline()
            .ConfigureSchemaServices(
                services => services.AddSingleton<IOperationDocumentNormalizer>(
                    sp => new CountingNormalizer(
                        new OperationDocumentNormalizer(
                            sp.GetRequiredService<ISchemaDefinition>(),
                            sp.GetRequiredService<NormalizedDocumentCache>()),
                        () => Interlocked.Increment(ref normalizeCallCount))))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string operationText =
            """
            query NormalizerCallCount {
              foo
            }
            """;

        // act
        var missResult = await executor.ExecuteAsync(operationText, TestContext.Current.CancellationToken);
        var countAfterMiss = Volatile.Read(ref normalizeCallCount);

        var hitResult = await executor.ExecuteAsync(operationText, TestContext.Current.CancellationToken);
        var countAfterHit = Volatile.Read(ref normalizeCallCount);

        // assert
        Assert.Empty(Assert.IsType<OperationResult>(missResult).Errors);
        Assert.Empty(Assert.IsType<OperationResult>(hitResult).Errors);
        Assert.Equal(1, countAfterMiss);
        Assert.Equal(1, countAfterHit);
    }

    [Fact]
    public async Task Normalized_Cache_Hit_On_Operation_Cache_Miss_Never_Rewrites()
    {
        // arrange
        // The prepared-operation cache is replaced with one that always misses, so every
        // request reaches the normalizer through an operation-cache miss. The normalizer
        // still keys its own NormalizedDocumentCache by operation id, so the second request
        // for the same operation finds the document the first request already rewrote and
        // never rewrites it again, even though the operation itself was never cached.
        var lookupCount = 0;
        var rewriteCount = 0;

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .UseDefaultPipeline()
            .ConfigureSchemaServices(
                services =>
                {
                    services.AddSingleton<IPreparedOperationCache>(new NeverHitPreparedOperationCache());
                    services.AddSingleton<IOperationDocumentNormalizer>(
                        sp => new RewriteCountingNormalizer(
                            new OperationDocumentNormalizer(
                                sp.GetRequiredService<ISchemaDefinition>(),
                                sp.GetRequiredService<NormalizedDocumentCache>()),
                            sp.GetRequiredService<NormalizedDocumentCache>(),
                            onLookup: () => Interlocked.Increment(ref lookupCount),
                            onRewrite: () => Interlocked.Increment(ref rewriteCount)));
                })
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string operationText =
            """
            query NormalizedCacheHitOnOperationCacheMiss {
              foo
            }
            """;

        // act
        var firstResult = await executor.ExecuteAsync(operationText, TestContext.Current.CancellationToken);
        var lookupCountAfterFirst = Volatile.Read(ref lookupCount);
        var rewriteCountAfterFirst = Volatile.Read(ref rewriteCount);

        var secondResult = await executor.ExecuteAsync(operationText, TestContext.Current.CancellationToken);
        var lookupCountAfterSecond = Volatile.Read(ref lookupCount);
        var rewriteCountAfterSecond = Volatile.Read(ref rewriteCount);

        // assert
        Assert.Empty(Assert.IsType<OperationResult>(firstResult).Errors);
        Assert.Empty(Assert.IsType<OperationResult>(secondResult).Errors);
        Assert.Equal(1, lookupCountAfterFirst);
        Assert.Equal(1, rewriteCountAfterFirst);

        // the second request is still an operation-cache miss, so it reaches the normalizer
        // exactly once more, but finds its document already cached and rewrites nothing.
        Assert.Equal(2, lookupCountAfterSecond);
        Assert.Equal(1, rewriteCountAfterSecond);
    }

    private sealed class CountingNormalizer(IOperationDocumentNormalizer inner, Action onNormalize)
        : IOperationDocumentNormalizer
    {
        public DocumentNode NormalizeDocument(RequestContext context)
        {
            onNormalize();
            return inner.NormalizeDocument(context);
        }
    }

    private sealed class RewriteCountingNormalizer(
        IOperationDocumentNormalizer inner,
        NormalizedDocumentCache normalizedDocumentCache,
        Action onLookup,
        Action onRewrite) : IOperationDocumentNormalizer
    {
        public DocumentNode NormalizeDocument(RequestContext context)
        {
            onLookup();

            context.TryGetOperationId(out var operationId);
            var hadCachedDocument = normalizedDocumentCache.TryGet(operationId!, out var documentCachedBefore);

            var normalizedDocument = inner.NormalizeDocument(context);

            if (!hadCachedDocument || !ReferenceEquals(documentCachedBefore, normalizedDocument))
            {
                onRewrite();
            }

            return normalizedDocument;
        }
    }

    private sealed class NeverHitPreparedOperationCache : IPreparedOperationCache
    {
        public int Capacity => 0;

        public int Count => 0;

        public bool TryGetOperation(string operationId, [NotNullWhen(true)] out Operation? operation)
        {
            operation = null;
            return false;
        }

        public void TryAddOperation(string operationId, Operation operation)
        {
        }
    }
}

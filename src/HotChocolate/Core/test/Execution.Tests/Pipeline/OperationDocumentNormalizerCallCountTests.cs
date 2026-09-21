using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

public sealed class OperationDocumentNormalizerCallCountTests
{
    [Fact]
    public async Task Fully_Cached_Request_Rewrites_Zero_Times_And_Compiles_Zero_Times()
    {
        // arrange: variable coercion now reads the normalized operation unconditionally, so
        // it reaches the normalizer on every request, cached or not. What must stay at zero
        // on a fully cached request (an operation cache hit whose document is already
        // normalized) is the actual rewrite work and the operation compilation, not the call
        // into the normalizer.
        var lookupCount = 0;
        var rewriteCount = 0;
        var compileCount = 0;

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve("bar"))
            .UseDefaultPipeline()
            .AddDiagnosticEventListener(_ => new CompileCountListener(() => Interlocked.Increment(ref compileCount)))
            .ConfigureSchemaServices(
                services => services.AddSingleton<IOperationDocumentNormalizer>(
                    sp => new RewriteCountingNormalizer(
                        new OperationDocumentNormalizer(
                            sp.GetRequiredService<ISchemaDefinition>(),
                            sp.GetRequiredService<NormalizedDocumentCache>()),
                        sp.GetRequiredService<NormalizedDocumentCache>(),
                        onLookup: () => Interlocked.Increment(ref lookupCount),
                        onRewrite: () => Interlocked.Increment(ref rewriteCount))))
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
        var lookupCountAfterMiss = Volatile.Read(ref lookupCount);
        var rewriteCountAfterMiss = Volatile.Read(ref rewriteCount);
        var compileCountAfterMiss = Volatile.Read(ref compileCount);

        var hitResult = await executor.ExecuteAsync(operationText, TestContext.Current.CancellationToken);
        var lookupCountAfterHit = Volatile.Read(ref lookupCount);
        var rewriteCountAfterHit = Volatile.Read(ref rewriteCount);
        var compileCountAfterHit = Volatile.Read(ref compileCount);

        // assert
        Assert.Empty(Assert.IsType<OperationResult>(missResult).Errors);
        Assert.Empty(Assert.IsType<OperationResult>(hitResult).Errors);
        Assert.Equal(1, lookupCountAfterMiss);
        Assert.Equal(1, rewriteCountAfterMiss);
        Assert.Equal(1, compileCountAfterMiss);

        // the fully cached request still reaches the normalizer, from variable coercion, but
        // its document is already normalized and its operation is already compiled, so
        // neither a rewrite nor a compile happens a second time.
        Assert.Equal(2, lookupCountAfterHit);
        Assert.Equal(1, rewriteCountAfterHit);
        Assert.Equal(1, compileCountAfterHit);
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

    private sealed class RewriteCountingNormalizer(
        IOperationDocumentNormalizer inner,
        NormalizedDocumentCache normalizedDocumentCache,
        Action onLookup,
        Action onRewrite) : IOperationDocumentNormalizer
    {
        public DocumentNode NormalizeDocument(RequestContext context)
        {
            onLookup();

            // Variable coercion may reach the normalizer before the operation cache stage
            // has run, so the operation id is not necessarily set yet; GetOperationId
            // creates and stores it on first access, exactly as the real normalizer does.
            var operationId = context.GetOperationId();
            var hadCachedDocument = normalizedDocumentCache.TryGet(operationId, out var documentCachedBefore);

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

    private sealed class CompileCountListener(Action onCompile) : ExecutionDiagnosticEventListener
    {
        public override IDisposable CompileOperation(RequestContext context)
        {
            onCompile();
            return EmptyScope;
        }
    }
}

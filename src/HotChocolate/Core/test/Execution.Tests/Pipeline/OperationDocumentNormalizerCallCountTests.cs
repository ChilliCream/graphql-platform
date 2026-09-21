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
        // arrange
        // Count cache lookups separately from document rewrites and operation compilations.
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

        Assert.Equal(2, lookupCountAfterHit);
        Assert.Equal(1, rewriteCountAfterHit);
        Assert.Equal(1, compileCountAfterHit);
    }

    [Fact]
    public async Task Normalized_Cache_Hit_On_Operation_Cache_Miss_Never_Rewrites()
    {
        // arrange
        // Force operation-cache misses to test the normalized-document cache independently.
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

            // Variable coercion can reach this before the operation cache assigns an id.
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

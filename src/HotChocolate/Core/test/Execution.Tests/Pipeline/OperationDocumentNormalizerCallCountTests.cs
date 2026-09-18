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
                        new OperationDocumentNormalizer(sp.GetRequiredService<ISchemaDefinition>()),
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

    private sealed class CountingNormalizer(IOperationDocumentNormalizer inner, Action onNormalize)
        : IOperationDocumentNormalizer
    {
        public DocumentNode NormalizeDocument(RequestContext context)
        {
            onNormalize();
            return inner.NormalizeDocument(context);
        }
    }
}

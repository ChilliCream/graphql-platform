using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Fusion.Execution.Caching;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class OperationDocumentNormalizerTests : FusionTestBase
{
    [Fact]
    public async Task Normalized_Document_Should_Inline_Fragments_And_Drop_Statically_Excluded_Selections()
    {
        // arrange
        DocumentNode? normalizedDocument = null;

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .UseRequest(
                (_, _) => context =>
                {
                    // Capture normalization output, then stop before execution needs a source-schema client.
                    normalizedDocument = context.GetNormalizedDocument();
                    context.Result =
                        new OperationResult(ImmutableOrderedDictionary<string, object?>.Empty.Add("probe", true));
                    return ValueTask.CompletedTask;
                },
                before: WellKnownRequestMiddleware.SkipWarmupExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                      bar: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string operationText =
            """
            query NormalizeMe {
              ...FooFragment
              bar @skip(if: true)
            }

            fragment FooFragment on Query {
              foo
            }
            """;

        // act
        var result = await executor.ExecuteAsync(operationText, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors ?? []);
        Assert.NotNull(normalizedDocument);
        normalizedDocument.MatchInlineSnapshot(
            """
            query NormalizeMe {
              foo
            }
            """);
    }

    [Fact]
    public async Task Normalized_Document_Should_Be_Cached_And_Reused_Across_Requests()
    {
        // arrange
        var normalizedDocuments = new List<DocumentNode>();
        var operationIds = new List<string>();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .UseRequest(
                (_, next) => context =>
                {
                    operationIds.Add(context.GetOperationId());
                    normalizedDocuments.Add(context.GetNormalizedDocument());
                    return next(context);
                },
                before: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string operationText =
            """
            query NormalizeMe {
              foo
            }
            """;

        // act
        await executor.ExecuteAsync(operationText, TestContext.Current.CancellationToken);
        await executor.ExecuteAsync(operationText, TestContext.Current.CancellationToken);
        await executor.ExecuteAsync(operationText, TestContext.Current.CancellationToken);

        var operationId = Assert.Single(operationIds.Distinct());
        var normalizedDocumentCache = executor.Schema.Services.GetRequiredService<NormalizedDocumentCache>();
        var found = normalizedDocumentCache.TryGet(operationId, out var cachedDocument);

        // assert
        Assert.True(found);
        Assert.NotNull(cachedDocument);
        Assert.Equal(3, normalizedDocuments.Count);
        Assert.Same(normalizedDocuments[0], normalizedDocuments[1]);
        Assert.Same(normalizedDocuments[1], normalizedDocuments[2]);
    }

    [Fact]
    public async Task Normalized_Document_Should_Be_Rewritten_When_Document_Has_Multiple_Operations()
    {
        // arrange
        var normalizedDocuments = new List<DocumentNode>();

        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .UseRequest(
                (_, _) => context =>
                {
                    // Capture normalization output, then stop before execution needs a source-schema client.
                    normalizedDocuments.Add(context.GetNormalizedDocument());
                    context.Result =
                        new OperationResult(ImmutableOrderedDictionary<string, object?>.Empty.Add("probe", true));
                    return ValueTask.CompletedTask;
                },
                before: WellKnownRequestMiddleware.SkipWarmupExecutionMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                      bar: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var documentHash = new OperationDocumentHash("multi-op-hash", "test", HashFormat.Hex);

        IOperationRequest CreateRequest(string operationName)
            => OperationRequestBuilder.New()
                .SetDocument("query A { foo } query B { bar }")
                .SetDocumentHash(documentHash)
                .SetOperationName(operationName)
                .Build();

        // act
        var resultA1 = await executor.ExecuteAsync(CreateRequest("A"), TestContext.Current.CancellationToken);
        var resultA2 = await executor.ExecuteAsync(CreateRequest("A"), TestContext.Current.CancellationToken);
        var resultB = await executor.ExecuteAsync(CreateRequest("B"), TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(resultA1.ExpectOperationResult().Errors ?? []);
        Assert.Empty(resultA2.ExpectOperationResult().Errors ?? []);
        Assert.Empty(resultB.ExpectOperationResult().Errors ?? []);
        Assert.Equal(3, normalizedDocuments.Count);
        normalizedDocuments[2].MatchInlineSnapshot(
            """
            query B {
              bar
            }
            """);
    }
}

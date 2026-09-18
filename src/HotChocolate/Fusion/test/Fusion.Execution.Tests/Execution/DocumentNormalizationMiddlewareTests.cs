using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Caching;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class DocumentNormalizationMiddlewareTests : FusionTestBase
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
                    // capture the normalized operation once it survived document normalization,
                    // the plan cache lookup and planning, then short-circuit before execution
                    // reaches out to a (non-existent) source schema client.
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
        // the first request rewrites the document and caches it under the operation id; every
        // later request for the same operation must reuse that cached instance instead of
        // rewriting the document again.
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
                    // capture the normalized operation once it survived document normalization,
                    // the plan cache lookup and planning, then short-circuit before execution
                    // reaches out to a (non-existent) source schema client.
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
        // requests for operation A must never leak a cached normalized body into a later
        // request for operation B on the same (multi-operation) document.
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

    [Fact]
    public async Task InvokeAsync_Should_ThrowCorrectMessage_When_DocumentIsMissing()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = services.AddGraphQLGateway();
        FusionSetupUtilities.ClearPipeline(builder);

        var executor = await builder
            .UseDocumentNormalization()
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

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.ExecuteAsync(
                "{ foo }",
                TestContext.Current.CancellationToken));

        // assert
        exception.Message.MatchInlineSnapshot(
            """
            The operation document is not available in the context.
            """);
    }
}

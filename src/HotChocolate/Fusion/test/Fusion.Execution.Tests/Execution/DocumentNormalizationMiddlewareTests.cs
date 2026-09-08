using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
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
    public async Task Normalized_Body_Should_Be_Cached_And_Reused_On_The_Next_Document_Cache_Hit()
    {
        // arrange
        var normalizedDocuments = new List<DocumentNode>();

        var executor = await new ServiceCollection()
            .AddHttpClient()
            .AddGraphQLGateway()
            .UseDefaultPipeline()
            .UseRequest(
                (_, next) => context =>
                {
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

        var documentHash = new OperationDocumentHash("normalize-me-hash", "test", HashFormat.Hex);

        IOperationRequest CreateRequest()
            => OperationRequestBuilder.New()
                .SetDocument("query NormalizeMe { foo }")
                .SetDocumentHash(documentHash)
                .Build();

        // act
        // the first request parses and normalizes the document; the second normalizes it again
        // and persists the normalized body onto the now-existing document cache entry; the third
        // must reuse that persisted normalized body instead of rewriting the document again.
        await executor.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);
        await executor.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);
        await executor.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var documentCache = executor.Schema.Services.GetRequiredService<IDocumentCache>();
        var found = documentCache.TryGetDocument(documentHash.Value, out var cachedDocument);

        // assert
        Assert.True(found);
        Assert.NotNull(cachedDocument!.NormalizedBody);
        Assert.Equal(3, normalizedDocuments.Count);
        Assert.Same(normalizedDocuments[1], normalizedDocuments[2]);
    }
}

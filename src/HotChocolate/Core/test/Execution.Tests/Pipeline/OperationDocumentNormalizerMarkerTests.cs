using HotChocolate.Execution.Internal;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

public sealed class OperationDocumentNormalizerMarkerTests
{
    [Fact]
    public async Task NormalizeDocument_Appends_Marker_When_Operation_Has_Defer()
    {
        // arrange
        DocumentNode? normalizedDocument = null;
        var executor = await CreateExecutorAsync(doc => normalizedDocument = doc);

        const string operationText =
            """
            query MarkerPresentWithDefer {
              hero {
                ... @defer {
                  name
                }
              }
            }
            """;

        // act
        await ExecuteAsync(executor, operationText);

        // assert
        Assert.True(HasIncrementalPartsMarker(normalizedDocument));
    }

    [Fact]
    public async Task NormalizeDocument_Does_Not_Append_Marker_When_Operation_Has_No_Defer_Or_Stream()
    {
        // arrange
        DocumentNode? normalizedDocument = null;
        var executor = await CreateExecutorAsync(doc => normalizedDocument = doc);

        const string operationText =
            """
            query MarkerAbsentWithoutDeferOrStream {
              hero {
                name
              }
            }
            """;

        // act
        await ExecuteAsync(executor, operationText);

        // assert
        Assert.False(HasIncrementalPartsMarker(normalizedDocument));
    }

    [Fact]
    public async Task NormalizeDocument_Carries_The_Marker_Through_A_Cache_Hit()
    {
        // arrange
        var normalizedDocuments = new List<DocumentNode>();
        var executor = await CreateExecutorAsync(normalizedDocuments.Add);

        const string operationText =
            """
            query MarkerSurvivesCacheHit {
              hero {
                ... @defer {
                  name
                }
              }
            }
            """;

        // act: the first request rewrites and caches the document; the second finds the same
        // operation id already in the normalized-document cache and returns that same document.
        await ExecuteAsync(executor, operationText);
        await ExecuteAsync(executor, operationText);

        // assert
        Assert.Equal(2, normalizedDocuments.Count);
        Assert.True(HasIncrementalPartsMarker(normalizedDocuments[0]));
        Assert.Same(normalizedDocuments[0], normalizedDocuments[1]);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(Action<DocumentNode> onNormalized)
        => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d => d
                    .Field("hero")
                    .Type<ObjectType<Hero>>()
                    .Resolve(new Hero()))
            .ModifyOptions(o => o.EnableDefer = true)
            .UseDefaultPipeline()
            .UseRequest(
                next => async context =>
                {
                    await next(context);
                    onNormalized(context.GetNormalizedDocument());
                },
                key: "CaptureNormalizedDocument")
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    private static async Task ExecuteAsync(IRequestExecutor executor, string operationText)
    {
        var result = await executor.ExecuteAsync(operationText, TestContext.Current.CancellationToken);

        // A deferred operation returns a response stream instead of a single result; draining
        // it exercises the same normalization the initial payload already went through.
        if (result is IResponseStream stream)
        {
            await foreach (var part in stream.ReadResultsAsync()
                .WithCancellation(TestContext.Current.CancellationToken))
            {
                await using var partCleanup = part;
                Assert.Empty(part.Errors);
            }

            return;
        }

        Assert.Empty(Assert.IsType<OperationResult>(result).Errors);
    }

    private static bool HasIncrementalPartsMarker(DocumentNode? normalizedDocument)
    {
        Assert.NotNull(normalizedDocument);

        var operationDefinition = (OperationDefinitionNode)normalizedDocument.Definitions[0];

        foreach (var directive in operationDefinition.Directives)
        {
            if (directive.Name.Value.Equals(
                InternalDirectiveNames.HasIncrementalParts, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public sealed class Hero
    {
        public string Name => "R2-D2";
    }
}

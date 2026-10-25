using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

public class DocumentCacheMiddlewareTests : FusionTestBase
{
    [Fact]
    public async Task InvokeAsync_Should_UseCachedDocument_When_RequestHasCachedDocumentId()
    {
        // arrange
        var listener = new DocumentCacheListener();
        var documentInfos = new List<object>();
        OperationDocumentId documentId = default;
        await using var services = new ServiceCollection()
            .AddHttpClient()
            .AddGraphQLGateway()
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => context =>
                {
                    var documentInfo = context.OperationDocumentInfo;
                    documentInfos.Add(
                        new
                        {
                            HasDocument = documentInfo.Document is not null,
                            documentInfo.IsCached,
                            documentInfo.IsValidated
                        });
                    return next(context);
                },
                after: WellKnownRequestMiddleware.DocumentCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => context =>
                {
                    documentId = context.OperationDocumentInfo.Id;
                    return next(context);
                },
                after: WellKnownRequestMiddleware.DocumentValidationMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      field: String
                    }
                    """))
            .Services
            .BuildServiceProvider();
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var warmupResult = await executor.ExecuteAsync(
            "{ __typename }",
            TestContext.Current.CancellationToken);
        using var request = OperationRequestBuilder.New().SetDocumentId(documentId).Build();
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Snapshot.Create()
            .Add(result, "Result")
            .Add(
                new
                {
                    listener.AddedToCache,
                    listener.RetrievedFromCache,
                    DocumentInfoAfterCache = documentInfos
                },
                "Document Cache")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task InvokeAsync_Should_UseCachedDocument_When_SourceTextWasValidatedBefore()
    {
        // arrange
        var listener = new DocumentCacheListener();
        var documentInfos = new List<object>();
        var documentIds = new List<string?>();
        var executor = await new ServiceCollection()
            .AddHttpClient()
            .AddGraphQLGateway()
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => context =>
                {
                    var documentInfo = context.OperationDocumentInfo;
                    documentInfos.Add(
                        new
                        {
                            HasDocument = documentInfo.Document is not null,
                            documentInfo.IsCached,
                            documentInfo.IsValidated
                        });
                    return next(context);
                },
                after: WellKnownRequestMiddleware.DocumentCacheMiddleware,
                allowMultiple: true)
            .UseRequest(
                (_, next) => context =>
                {
                    documentIds.Add(context.OperationDocumentInfo.Id.Value);
                    return next(context);
                },
                after: WellKnownRequestMiddleware.DocumentValidationMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      field: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await executor.ExecuteAsync("{ __typename }", TestContext.Current.CancellationToken);
        var result = await executor.ExecuteAsync("{ __typename }", TestContext.Current.CancellationToken);

        // assert
        Snapshot.Create()
            .Add(result, "Result")
            .Add(
                new
                {
                    listener.AddedToCache,
                    listener.RetrievedFromCache,
                    DocumentInfoAfterCache = documentInfos,
                    DocumentIdAfterValidation = documentIds
                },
                "Document Cache")
            .MatchMarkdownSnapshot();
    }

    private sealed class DocumentCacheListener : FusionExecutionDiagnosticEventListener
    {
        private int _addedToCache;
        private int _retrievedFromCache;

        public int AddedToCache => _addedToCache;

        public int RetrievedFromCache => _retrievedFromCache;

        public override void AddedDocumentToCache(RequestContext context)
            => Interlocked.Increment(ref _addedToCache);

        public override void RetrievedDocumentFromCache(RequestContext context)
            => Interlocked.Increment(ref _retrievedFromCache);
    }
}

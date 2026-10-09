using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.PersistedOperations.InMemory;

public class IntegrationTests
{
    [Fact]
    public async Task ExecutePersistedOperation_Should_ReturnEmptyData_When_RootSelectionSetBypassesValidation()
    {
        // arrange
        var executor = await CreateRequestExecutorAsync();
        await SaveOperationDocumentAsync(executor, "empty-root", "{ }");

        // act
        var result = await executor.ExecuteAsync(
            OperationRequest.FromId("empty-root"),
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {}
            }
            """);
    }

    [Fact]
    public async Task ExecutePersistedOperation_Should_ReturnEmptyData_When_NestedSelectionSetBypassesValidation()
    {
        // arrange
        var executor = await CreateRequestExecutorAsync();
        await SaveOperationDocumentAsync(executor, "empty-nested", "{ hero { } }");

        // act
        var result = await executor.ExecuteAsync(
            OperationRequest.FromId("empty-nested"),
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {}
              }
            }
            """);
    }

    [Fact]
    public async Task ExecutePersistedOperation_Should_ReturnError_When_SubscriptionSelectionSetBypassesValidation()
    {
        // arrange
        var executor = await CreateRequestExecutorAsync();
        await SaveOperationDocumentAsync(executor, "empty-subscription", "subscription { }");

        // act
        var result = await executor.ExecuteAsync(
            OperationRequest.FromId("empty-subscription"),
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Subscription queries must have exactly one root field."
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task ExecutePersistedOperation()
    {
        // arrange
        var documentId = Guid.NewGuid().ToString("N");
        var document = Utf8GraphQLParser.Parse("{ __typename }");

        IServiceProvider services =
            new ServiceCollection()
                .AddMemoryCache()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddInMemoryOperationDocumentStorage()
                .UseRequest((_, n) => async c =>
                {
                    await n(c);

                    if (c.IsPersistedOperationDocument())
                    {
                        var result = c.Result.ExpectOperationResult();
                        result.Extensions = result.Extensions.SetItem("persistedDocument", true);
                    }
                })
                .UsePersistedOperationPipeline()
                .Services
                .BuildServiceProvider();

        var cache = services.GetRequiredService<IMemoryCache>();
        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        cache.GetOrCreate(documentId, _ => new OperationDocument(document));

        // act
        var result = await executor.ExecuteAsync(
            OperationRequest.FromId(documentId),
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_NotFound()
    {
        // arrange
        IServiceProvider services =
            new ServiceCollection()
                .AddMemoryCache()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddInMemoryOperationDocumentStorage()
                .UseRequest((_, n) => async c =>
                {
                    await n(c);

                    if (c.IsPersistedOperationDocument())
                    {
                        var result = c.Result.ExpectOperationResult();
                        result.Extensions = result.Extensions.SetItem("persistedDocument", true);
                    }
                })
                .UsePersistedOperationPipeline()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequest.FromId("does_not_exist"),
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot();
    }

    private static async Task<IRequestExecutor> CreateRequestExecutorAsync()
    {
        var services = new ServiceCollection()
            .AddMemoryCache()
            .AddGraphQL()
            .ModifyRequestOptions(o => o.PersistedOperations.SkipPersistedDocumentValidation = true)
            .AddQueryType<Query>()
            .AddSubscriptionType(
                d => d.Field("onDroid")
                    .Type<StringType>()
                    .Resolve(_ => new ValueTask<object?>("R2-D2")))
            .AddInMemoryOperationDocumentStorage()
            .UsePersistedOperationPipeline()
            .Services
            .BuildServiceProvider();

        return await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
    }

    private static async Task SaveOperationDocumentAsync(
        IRequestExecutor executor,
        string documentId,
        string document)
    {
        var storage = executor.Schema.Services.GetRequiredService<IOperationDocumentStorage>();
        await storage.SaveAsync(
            new OperationDocumentId(documentId),
            new OperationDocument(Utf8GraphQLParser.Parse(document)),
            TestContext.Current.CancellationToken);
    }

    public sealed class Query
    {
        public Droid Hero() => new();
    }

    public sealed class Droid
    {
        public string Name => "R2-D2";
    }
}

using HotChocolate.Execution.Caching;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotChocolate.Execution;

public class WarmupRequestTests
{
    [Fact]
    public async Task Warmup_Request_Warms_Up_Caches()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        var documentId = "f614e9a2ed367399e87751d41ca09105";
        var warmupRequest = OperationRequestBuilder.New()
            .SetDocument("query test($name: String!) { greeting(name: $name) }")
            .SetDocumentId(documentId)
            .MarkAsWarmupRequest()
            .Build();

        var regularRequest = OperationRequestBuilder.New()
            .SetDocumentId(documentId)
            .SetVariableValues(new Dictionary<string, object?> { ["name"] = "Foo" })
            .Build();

        // act 1
        var warmupResult = await executor.ExecuteAsync(warmupRequest);

        // assert 1
        Assert.IsType<WarmupExecutionResult>(warmupResult);

        var provider = executor.Services.GetCombinedServices();
        var documentCache = provider.GetRequiredService<IDocumentCache>();
        var operationCache = provider.GetRequiredService<IPreparedOperationCache>();

        Assert.True(documentCache.TryGetDocument(documentId, out _));
        Assert.Equal(1, operationCache.Count);

        // act 2
        var regularResult = await executor.ExecuteAsync(regularRequest);
        var regularOperationResult = regularResult.ExpectOperationResult();

        // assert 2
        Assert.Null(regularOperationResult.Errors);
        Assert.NotNull(regularOperationResult.Data);
        Assert.NotEmpty(regularOperationResult.Data);

        Assert.True(documentCache.TryGetDocument(documentId, out _));
        Assert.Equal(1, operationCache.Count);
    }

    [Fact]
    public async Task Warmup_Request_Can_Skip_Persisted_Operation_Check()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .ConfigureSchemaServices(services =>
            {
                services.AddSingleton<IOperationDocumentStorage>(_ => new Mock<IOperationDocumentStorage>().Object);
            })
            .AddQueryType<Query>()
            .ModifyRequestOptions(options =>
            {
                options.PersistedOperations.OnlyAllowPersistedDocuments = true;
            })
            .UsePersistedOperationPipeline()
            .BuildRequestExecutorAsync();

        var documentId = "f614e9a2ed367399e87751d41ca09105";
        var warmupRequest = OperationRequestBuilder.New()
            .SetDocument("query test($name: String!) { greeting(name: $name) }")
            .SetDocumentId(documentId)
            .MarkAsWarmupRequest()
            .Build();

        // act
        var warmupResult = await executor.ExecuteAsync(warmupRequest);

        // assert
        Assert.IsType<WarmupExecutionResult>(warmupResult);

        var provider = executor.Services.GetCombinedServices();
        var documentCache = provider.GetRequiredService<IDocumentCache>();
        var operationCache = provider.GetRequiredService<IPreparedOperationCache>();

        Assert.True(documentCache.TryGetDocument(documentId, out _));
        Assert.Equal(1, operationCache.Count);
    }

    public class Query
    {
        public string Greeting(string name) => $"Hello {name}";
    }
}

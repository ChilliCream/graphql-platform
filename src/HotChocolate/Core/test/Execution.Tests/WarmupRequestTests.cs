using HotChocolate.Execution.Caching;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;
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
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string documentId = "f614e9a2ed367399e87751d41ca09105";
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
        var warmupResult = await executor.ExecuteAsync(warmupRequest, TestContext.Current.CancellationToken);

        // assert 1
        Assert.IsType<WarmupExecutionResult>(warmupResult);

        var documentCache = executor.Schema.Services.GetRequiredService<IDocumentCache>();
        var operationCache = executor.Schema.Services.GetRequiredService<IPreparedOperationCache>();

        Assert.True(documentCache.TryGetDocument(documentId, out _));
        Assert.Equal(1, operationCache.Count);

        // act 2
        var regularResult = await executor.ExecuteAsync(regularRequest, TestContext.Current.CancellationToken);
        var regularOperationResult = regularResult.ExpectOperationResult();

        // assert 2
        Assert.Empty(regularOperationResult.Errors);
        Assert.True(regularOperationResult.UnwrapData().EnumerateObject().Any());

        Assert.True(documentCache.TryGetDocument(documentId, out _));
        Assert.Equal(1, operationCache.Count);
    }

    [Fact]
    public async Task Warmup_Request_Can_Skip_Persisted_Operation_Check()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .ConfigureSchemaServices(
                services =>
                    services.AddSingleton(_ => new Mock<IOperationDocumentStorage>().Object))
            .AddQueryType<Query>()
            .ModifyRequestOptions(
                options => options.PersistedOperations.OnlyAllowPersistedDocuments = true)
            .UsePersistedOperationPipeline()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string documentId = "f614e9a2ed367399e87751d41ca09105";
        var warmupRequest = OperationRequestBuilder.New()
            .SetDocument("query test($name: String!) { greeting(name: $name) }")
            .SetDocumentId(documentId)
            .MarkAsWarmupRequest()
            .Build();

        // act
        var warmupResult = await executor.ExecuteAsync(warmupRequest, TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<WarmupExecutionResult>(warmupResult);

        var provider = executor.Schema.Services;
        var documentCache = provider.GetRequiredService<IDocumentCache>();
        var operationCache = provider.GetRequiredService<IPreparedOperationCache>();

        Assert.True(documentCache.TryGetDocument(documentId, out _));
        Assert.Equal(1, operationCache.Count);
    }

    [Fact]
    public async Task Cost_Validation_Request_Without_Variables_Skips_Coercion()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // a required variable is declared but no value is supplied, which would normally
        // make coercion throw; here it must be skipped because the request only validates cost.
        var request = OperationRequestBuilder.New()
            .SetDocument("query test($name: String!) { greeting(name: $name) }")
            .AddGlobalState(ExecutionContextData.ValidateCost, true)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Equal(
            "Either no compiled operation was found or the variables have not been coerced.",
            Assert.Single(operationResult.Errors!).Message);
    }

    public class Query
    {
        public string Greeting(string name) => $"Hello {name}";
    }
}

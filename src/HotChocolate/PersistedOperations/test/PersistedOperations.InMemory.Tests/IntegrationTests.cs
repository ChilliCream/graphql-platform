using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.PersistedOperations.InMemory;

public class IntegrationTests
{
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
                .UseRequest(n => async c =>
                {
                    await n(c);

                    if (c.IsPersistedDocument && c.Result is IOperationResult r)
                    {
                        c.Result = OperationResultBuilder
                            .FromResult(r)
                            .SetExtension("persistedDocument", true)
                            .Build();
                    }
                })
                .UsePersistedOperationPipeline()
                .Services
                .BuildServiceProvider();

        var cache = services.GetRequiredService<IMemoryCache>();
        var executor = await services.GetRequestExecutorAsync();

        cache.GetOrCreate(documentId, _ => new OperationDocument(document));

        // act
        var result = await executor.ExecuteAsync(OperationRequest.FromId(documentId));

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
                .UseRequest(n => async c =>
                {
                    await n(c);

                    if (c.IsPersistedDocument && c.Result is IOperationResult r)
                    {
                        c.Result = OperationResultBuilder
                            .FromResult(r)
                            .SetExtension("persistedDocument", true)
                            .Build();
                    }
                })
                .UsePersistedOperationPipeline()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(OperationRequest.FromId("does_not_exist"));

        // assert
        result.ToJson().MatchSnapshot();
    }
}

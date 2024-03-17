using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;

namespace HotChocolate.PersistedQueries.InMemory;

public class IntegrationTests
{
    [Fact]
    public async Task ExecutePersistedQuery()
    {
        // arrange
        var queryId = Guid.NewGuid().ToString("N");
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
                .UsePersistedQueryPipeline()
                .Services
                .BuildServiceProvider();

        var cache = services.GetRequiredService<IMemoryCache>();
        var executor = await services.GetRequestExecutorAsync();

        cache.GetOrCreate(queryId, _ => new OperationDocument(document));

        // act
        var result = await executor.ExecuteAsync(OperationRequest.FromId(queryId));

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedQuery_NotFound()
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
                .UsePersistedQueryPipeline()
                .Services
                .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(OperationRequest.FromId("does_not_exist"));

        // assert
        result.ToJson().MatchSnapshot();
    }
}

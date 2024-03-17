using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using IO = System.IO;

namespace HotChocolate.PersistedQueries.FileSystem;

public class IntegrationTests
{
    [Fact]
    public async Task ExecutePersistedQuery()
    {
        // arrange
        var queryId = Guid.NewGuid().ToString("N");
        var cacheDirectory = IO.Path.GetTempPath();
        var cachedQuery = IO.Path.Combine(cacheDirectory, queryId + ".graphql");

        await File.WriteAllTextAsync(cachedQuery, "{ __typename }");

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddFileSystemQueryStorage(cacheDirectory)
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
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(OperationRequest.FromId(queryId));

        // assert
        File.Delete(cachedQuery);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedQuery_NotFound()
    {
        // arrange
        var queryId = Guid.NewGuid().ToString("N");
        var cacheDirectory = IO.Path.GetTempPath();
        var cachedQuery = IO.Path.Combine(cacheDirectory, queryId + ".graphql");

        await File.WriteAllTextAsync(cachedQuery, "{ __typename }");

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddFileSystemQueryStorage(cacheDirectory)
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
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(OperationRequest.FromId("does_not_exist"));

        // assert
        File.Delete(cachedQuery);
        result.MatchSnapshot();
    }
}

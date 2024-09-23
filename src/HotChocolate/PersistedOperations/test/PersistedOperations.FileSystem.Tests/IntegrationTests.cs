using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using IO = System.IO;

namespace HotChocolate.PersistedOperations.FileSystem;

public class IntegrationTests
{
    [Fact]
    public async Task ExecutePersistedOperation()
    {
        // arrange
        var documentId = Guid.NewGuid().ToString("N");
        var cacheDirectory = IO.Path.GetTempPath();
        var cachedOperation = IO.Path.Combine(cacheDirectory, documentId + ".graphql");

        await File.WriteAllTextAsync(cachedOperation, "{ __typename }");

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddFileSystemOperationDocumentStorage(cacheDirectory)
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
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(OperationRequest.FromId(documentId));

        // assert
        File.Delete(cachedOperation);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_NotFound()
    {
        // arrange
        var documentId = Guid.NewGuid().ToString("N");
        var cacheDirectory = IO.Path.GetTempPath();
        var cachedOperation = IO.Path.Combine(cacheDirectory, documentId + ".graphql");

        await File.WriteAllTextAsync(cachedOperation, "{ __typename }");

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddFileSystemOperationDocumentStorage(cacheDirectory)
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
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(OperationRequest.FromId("does_not_exist"));

        // assert
        File.Delete(cachedOperation);
        result.MatchSnapshot();
    }
}

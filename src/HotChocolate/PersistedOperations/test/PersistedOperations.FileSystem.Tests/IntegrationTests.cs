using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
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

        await File.WriteAllTextAsync(cachedOperation, "{ __typename }", TestContext.Current.CancellationToken);

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddFileSystemOperationDocumentStorage(cacheDirectory)
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
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequest.FromId(documentId),
            TestContext.Current.CancellationToken);

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

        await File.WriteAllTextAsync(cachedOperation, "{ __typename }", TestContext.Current.CancellationToken);

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddFileSystemOperationDocumentStorage(cacheDirectory)
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
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequest.FromId("does_not_exist"),
            TestContext.Current.CancellationToken);

        // assert
        File.Delete(cachedOperation);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAutomaticPersistedOperation()
    {
        // arrange
        var cacheDirectory = IO.Path.GetTempPath();
        const string documentHash = "hash";

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddFileSystemOperationDocumentStorage(cacheDirectory)
                .UseRequest((_, n) => async c =>
                {
                    await n(c);

                    if (c.IsPersistedOperationDocument())
                    {
                        var result = c.Result.ExpectOperationResult();
                        result.Extensions = result.Extensions.SetItem("persistedDocument", true);
                    }
                })
                .UseAutomaticPersistedOperationPipeline()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocumentId(documentHash)
                .SetDocument(Utf8GraphQLParser.Parse("{ __typename }"))
                .SetDocumentHash(new OperationDocumentHash(documentHash, "MD5", HashFormat.Base64))
                .SetExtensions(new Dictionary<string, object?>
                {
                    {
                        "persistedQuery",
                        new Dictionary<string, object?>
                        {
                            { "version", 1 },
                            { "md5Hash", documentHash }
                        }
                    }
                })
                .Build(),
            TestContext.Current.CancellationToken);

        File.Delete(IO.Path.Combine(cacheDirectory, documentHash + ".graphql"));

        // assert
        result.MatchSnapshot();
    }
}

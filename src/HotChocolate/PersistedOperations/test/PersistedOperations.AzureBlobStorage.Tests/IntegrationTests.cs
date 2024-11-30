using Azure.Storage.Blobs;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.PersistedOperations.AzureBlobStorage;

public class IntegrationTests : IClassFixture<AzureStorageBlobResource>
{
    private const string Prefix = "hc_";
    private const string Suffix = ".graphql";

    private readonly BlobContainerClient _client;

    public IntegrationTests(AzureStorageBlobResource blobStorageResource)
    {
        _client = blobStorageResource.CreateBlobServiceClient().GetBlobContainerClient("test");
        _client.CreateIfNotExists();
    }

    [Fact]
    public async Task ExecutePersistedOperation()
    {
        // arrange
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));
        var storage = new AzureBlobOperationDocumentStorage(_client, Prefix, Suffix);

        await storage.SaveAsync(
            documentId,
            new OperationDocumentSourceText("{ __typename }"));

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddAzureBlobStorageOperationDocumentStorage(_ => _client, Prefix, Suffix)
                .UseRequest(n => async c =>
                {
                    await n(c);

                    if (c is { IsPersistedDocument: true, Result: IOperationResult r })
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
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_NotFound()
    {
        // arrange
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));
        var storage = new AzureBlobOperationDocumentStorage(_client, Prefix, Suffix);
        await storage.SaveAsync(documentId, new OperationDocumentSourceText("{ __typename }"));

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddAzureBlobStorageOperationDocumentStorage(_ => _client, Prefix, Suffix)
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
        var result =
            await executor.ExecuteAsync(OperationRequest.FromId("does_not_exist"));

        // assert
        result.MatchSnapshot();
    }
}

using Azure.Storage.Blobs;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.PersistedOperations.AzureBlobStorage;

public class IntegrationTests : IClassFixture<AzureStorageBlobResource>
{
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
        var storage = new AzureBlobOperationDocumentStorage(_client);

        await storage.SaveAsync(
            documentId,
            new OperationDocumentSourceText("{ __typename }"));

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddAzureBlobStorageOperationDocumentStorage(_ => _client)
                .UseRequest((_, n) => async c =>
                {
                    await n(c);

                    if (c.IsPersistedOperationDocument() && c.Result is OperationResult result)
                    {
                        result.ContextData = result.ContextData.SetItem("persistedDocument", true);
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
        var storage = new AzureBlobOperationDocumentStorage(_client);
        await storage.SaveAsync(documentId, new OperationDocumentSourceText("{ __typename }"));

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddAzureBlobStorageOperationDocumentStorage(_ => _client)
                .UseRequest((_, n) => async c =>
                {
                    await n(c);

                    if (c.IsPersistedOperationDocument())
                    {
                        var result = c.Result.ExpectOperationResult();
                        var extensions = result.Extensions ?? [];
                        result.Extensions = extensions.SetItem("persistedDocument", true);
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

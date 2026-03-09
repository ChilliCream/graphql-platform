using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.PersistedOperations.AzureBlobStorage;

public class RequestExecutorBuilderTests : IClassFixture<AzureStorageBlobResource>
{
    private readonly BlobContainerClient _client;

    public RequestExecutorBuilderTests(AzureStorageBlobResource blobStorageResource)
    {
        _client = blobStorageResource.CreateBlobServiceClient().GetBlobContainerClient("test");
        _client.CreateIfNotExists();
    }

    [Fact]
    public void AddAzureBlobStorageOperationDocumentStorage_Services_Is_Null()
    {
        // arrange
        // act
        void Action() =>
            HotChocolateAzureBlobStoragePersistedOperationsRequestExecutorBuilderExtensions
                .AddAzureBlobStorageOperationDocumentStorage(null!, _ => _client);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddAzureBlobStorageOperationDocumentStorage_Factory_Is_Null()
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL();

        // act
        void Action() =>
            builder.AddAzureBlobStorageOperationDocumentStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }
}

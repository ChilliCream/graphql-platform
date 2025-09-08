using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Utilities;
using Squadron;

namespace HotChocolate.PersistedOperations.AzureBlobStorage;

public class ServiceCollectionExtensionsTests : IClassFixture<AzureStorageBlobResource>
{
    private readonly BlobContainerClient _client;

    public ServiceCollectionExtensionsTests(AzureStorageBlobResource blobStorageResource)
    {
        _client = blobStorageResource.CreateBlobServiceClient().GetBlobContainerClient("test");
        _client.CreateIfNotExists();
    }

    [Fact]
    public void AddAzureBlobStorageOperationDocumentStorage_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateAzureBlobStoragePersistedOperationsServiceCollectionExtensions
                .AddAzureBlobStorageOperationDocumentStorage(null!, _ => _client);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddAzureBlobStorageOperationDocumentStorage_Factory_Is_Null()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        void Action()
            => services.AddAzureBlobStorageOperationDocumentStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddAzureBlobStorageOperationDocumentStorage_Services()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddAzureBlobStorageOperationDocumentStorage(_ => _client);

        // assert
        services.ToDictionary(
                k => k.ServiceType.GetTypeName(),
                v => v.ImplementationType?.GetTypeName())
            .OrderBy(t => t.Key)
            .MatchSnapshot();
    }
}

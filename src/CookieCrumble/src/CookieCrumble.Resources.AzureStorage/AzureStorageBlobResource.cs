using Azure.Storage.Blobs;
using Testcontainers.Azurite;

namespace CookieCrumble.Resources;

public class AzureStorageBlobResource : ContainerResource<AzuriteContainer>
{
    public string ConnectionString => Container.GetConnectionString();

    public BlobServiceClient CreateBlobServiceClient() => new(ConnectionString);

    protected override AzuriteContainer Build()
        => Configure(new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.33.0")).Build();

    protected virtual AzuriteBuilder Configure(AzuriteBuilder builder) => builder;
}

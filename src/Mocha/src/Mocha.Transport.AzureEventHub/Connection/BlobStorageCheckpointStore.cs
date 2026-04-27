using System.Text;
using Azure;
using Azure.Storage.Blobs;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Tracks last processed sequence number per partition in Azure Blob Storage.
/// Provides at-least-once delivery that survives process restarts.
/// </summary>
public sealed class BlobStorageCheckpointStore : ICheckpointStore
{
    private readonly BlobContainerClient _containerClient;

    /// <summary>
    /// Creates a new blob storage checkpoint store.
    /// </summary>
    /// <param name="containerClient">The blob container client for storing checkpoint blobs.</param>
    public BlobStorageCheckpointStore(BlobContainerClient containerClient)
    {
        _containerClient = containerClient;
    }

    /// <inheritdoc />
    public async ValueTask<long?> GetCheckpointAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId,
        CancellationToken cancellationToken)
    {
        var blobClient = GetBlobClient(fullyQualifiedNamespace, eventHubName, consumerGroup, partitionId);

        try
        {
            var response = await blobClient.DownloadContentAsync(cancellationToken);
            var content = response.Value.Content;

            if (long.TryParse(content.ToString(), out var sequenceNumber))
            {
                return sequenceNumber;
            }

            return null;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async ValueTask SetCheckpointAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId,
        long sequenceNumber,
        CancellationToken cancellationToken)
    {
        var blobClient = GetBlobClient(fullyQualifiedNamespace, eventHubName, consumerGroup, partitionId);
        var content = new BinaryData(Encoding.UTF8.GetBytes(sequenceNumber.ToString()));

        await blobClient.UploadAsync(content, overwrite: true, cancellationToken);
    }

    private BlobClient GetBlobClient(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId)
    {
        var blobName = $"{fullyQualifiedNamespace}/{eventHubName}/{consumerGroup}/checkpoint/{partitionId}";
        return _containerClient.GetBlobClient(blobName);
    }
}

using Azure;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Coordinates partition ownership across multiple processor instances using Azure Blob Storage.
/// Each partition ownership is stored as a blob with metadata, using ETags for optimistic concurrency.
/// </summary>
public sealed class BlobStorageOwnershipStore : IPartitionOwnershipStore
{
    private const string OwnerIdentifierMetadataKey = "ownerid";

    private readonly BlobContainerClient _containerClient;

    /// <summary>
    /// Creates a new blob storage ownership store.
    /// </summary>
    /// <param name="containerClient">The blob container client for storing ownership blobs.</param>
    public BlobStorageOwnershipStore(BlobContainerClient containerClient)
    {
        _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<EventProcessorPartitionOwnership>> ListOwnershipAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        CancellationToken cancellationToken)
    {
        var prefix = BuildOwnershipPrefix(fullyQualifiedNamespace, eventHubName, consumerGroup);
        var result = new List<EventProcessorPartitionOwnership>();

        await foreach (var blob in _containerClient.GetBlobsByHierarchyAsync(
            traits: BlobTraits.Metadata,
            prefix: prefix,
            cancellationToken: cancellationToken))
        {
            if (blob.IsBlob
                && blob.Blob.Metadata.TryGetValue(OwnerIdentifierMetadataKey, out var ownerIdentifier))
            {
                var partitionId = blob.Blob.Name[(prefix.Length)..];

                result.Add(new EventProcessorPartitionOwnership
                {
                    FullyQualifiedNamespace = fullyQualifiedNamespace,
                    EventHubName = eventHubName,
                    ConsumerGroup = consumerGroup,
                    PartitionId = partitionId,
                    OwnerIdentifier = ownerIdentifier,
                    LastModifiedTime = blob.Blob.Properties.LastModified ?? DateTimeOffset.UtcNow,
                    Version = blob.Blob.Properties.ETag?.ToString()
                });
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<EventProcessorPartitionOwnership>> ClaimOwnershipAsync(
        IEnumerable<EventProcessorPartitionOwnership> desiredOwnership,
        CancellationToken cancellationToken)
    {
        var claimed = new List<EventProcessorPartitionOwnership>();

        foreach (var ownership in desiredOwnership)
        {
            try
            {
                var blobClient = GetOwnershipBlobClient(
                    ownership.FullyQualifiedNamespace,
                    ownership.EventHubName,
                    ownership.ConsumerGroup,
                    ownership.PartitionId);

                var metadata = new Dictionary<string, string>
                {
                    [OwnerIdentifierMetadataKey] = ownership.OwnerIdentifier
                };

                BlobInfo blobInfo;

                if (string.IsNullOrEmpty(ownership.Version))
                {
                    // New claim: create or overwrite only if the blob doesn't exist.
                    var content = BinaryData.FromString(string.Empty);
                    var response = await blobClient.UploadAsync(
                        content,
                        new BlobUploadOptions
                        {
                            Metadata = metadata,
                            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
                        },
                        cancellationToken);

                    blobInfo = new BlobInfo
                    {
                        ETag = response.Value.ETag,
                        LastModified = response.Value.LastModified
                    };
                }
                else
                {
                    // Existing claim: update only if the ETag matches (optimistic concurrency).
                    var response = await blobClient.SetMetadataAsync(
                        metadata,
                        new BlobRequestConditions { IfMatch = new ETag(ownership.Version) },
                        cancellationToken);

                    blobInfo = new BlobInfo
                    {
                        ETag = response.Value.ETag,
                        LastModified = response.Value.LastModified
                    };
                }

                claimed.Add(new EventProcessorPartitionOwnership
                {
                    FullyQualifiedNamespace = ownership.FullyQualifiedNamespace,
                    EventHubName = ownership.EventHubName,
                    ConsumerGroup = ownership.ConsumerGroup,
                    PartitionId = ownership.PartitionId,
                    OwnerIdentifier = ownership.OwnerIdentifier,
                    LastModifiedTime = blobInfo.LastModified,
                    Version = blobInfo.ETag.ToString()
                });
            }
            catch (RequestFailedException ex) when (ex.Status is 409 or 412)
            {
                // ETag conflict or precondition failed: another instance claimed this partition.
                // Skip it — this is expected during distributed coordination.
            }
        }

        return claimed;
    }

    private BlobClient GetOwnershipBlobClient(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId)
    {
        var blobName = $"{fullyQualifiedNamespace}/{eventHubName}/{consumerGroup}/ownership/{partitionId}";
        return _containerClient.GetBlobClient(blobName);
    }

    private static string BuildOwnershipPrefix(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup)
        => $"{fullyQualifiedNamespace}/{eventHubName}/{consumerGroup}/ownership/";

    private sealed record BlobInfo
    {
        public ETag ETag { get; init; }
        public DateTimeOffset LastModified { get; init; }
    }
}

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.PersistedOperations.AzureBlobStorage;

/// <summary>
/// An implementation of <see cref="IOperationDocumentStorage"/> that uses Redis as a storage.
/// </summary>
public class AzureBlobOperationDocumentStorage : IOperationDocumentStorage
{
    private static readonly BlobOpenWriteOptions _defaultBlobOpenWriteOptions = new()
    {
        HttpHeaders = new BlobHttpHeaders
        {
            ContentType = "application/graphql",
            ContentDisposition = "inline",
            CacheControl = "public, max-age=604800, immutable"
        }
    };

    private readonly BlobContainerClient _blobContainerClient;
    private readonly string _blobNamePrefix;
    private readonly string _blobNameSuffix;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="containerClient">The blob container client instance.</param>
    /// <param name="blobNamePrefix">This prefix string is prepended before the hash of the document.</param>
    /// <param name="blobNameSuffix">This suffix is appended after the hash of the document.</param>
    public AzureBlobOperationDocumentStorage(
        BlobContainerClient containerClient,
        string blobNamePrefix,
        string blobNameSuffix)
    {
        ArgumentNullException.ThrowIfNull(containerClient);
        ArgumentNullException.ThrowIfNull(blobNamePrefix);
        ArgumentNullException.ThrowIfNull(blobNameSuffix);

        _blobContainerClient = containerClient;
        _blobNamePrefix = blobNamePrefix;
        _blobNameSuffix = blobNameSuffix;
    }

    /// <inheritdoc />
    public ValueTask<IOperationDocument?> TryReadAsync(
        OperationDocumentId documentId,
        CancellationToken cancellationToken = default)
    {
        if (OperationDocumentId.IsNullOrEmpty(documentId))
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        return TryReadInternalAsync(documentId, cancellationToken);
    }

    private async ValueTask<IOperationDocument?> TryReadInternalAsync(
        OperationDocumentId documentId,
        CancellationToken cancellationToken)
    {
        var blobClient = _blobContainerClient.GetBlobClient(BlobName(documentId));

        try
        {
            await using var blobStream = await blobClient
                .OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            await using var memoryStream = new MemoryStream();
            await blobStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            return memoryStream.Length == 0
                ? null
                : new OperationDocument(Utf8GraphQLParser.Parse(memoryStream.ToArray()));
        }
        catch (RequestFailedException e)
        {
            if (e.Status == 404)
            {
                return null;
            }

            throw;
        }
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(
        OperationDocumentId documentId,
        IOperationDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (OperationDocumentId.IsNullOrEmpty(documentId))
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        return SaveInternalAsync(documentId, document, cancellationToken);
    }

    private async ValueTask SaveInternalAsync(
        OperationDocumentId documentId,
        IOperationDocument document,
        CancellationToken cancellationToken)
    {
        var blobClient = _blobContainerClient.GetBlobClient(BlobName(documentId));
        await using var outStream = await blobClient
            .OpenWriteAsync(true, _defaultBlobOpenWriteOptions, cancellationToken).ConfigureAwait(false);

        await document.WriteToAsync(outStream, cancellationToken).ConfigureAwait(false);
        await outStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private string BlobName(OperationDocumentId documentId) => $"{_blobNamePrefix}{documentId.Value}{_blobNameSuffix}";
}

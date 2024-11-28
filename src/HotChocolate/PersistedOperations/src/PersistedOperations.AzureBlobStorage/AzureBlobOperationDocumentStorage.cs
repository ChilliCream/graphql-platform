using Azure.Storage.Blobs;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.PersistedOperations.AzureBlobStorage;

/// <summary>
/// An implementation of <see cref="IOperationDocumentStorage"/> that uses Redis as a storage.
/// </summary>
public class AzureBlobOperationDocumentStorage : IOperationDocumentStorage
{
    private readonly BlobContainerClient _blobContainerClient;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="containerClient">The blob container client instance.</param>
    public AzureBlobOperationDocumentStorage(BlobContainerClient containerClient)
    {
        _blobContainerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
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
        var blobClient = _blobContainerClient.GetBlobClient(documentId.Value);
        if (!await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        await using var memoryStream = new MemoryStream();
        await using var blobStream = await blobClient
            .OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        await blobStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

        return memoryStream.Length == 0 ? null : new OperationDocument(Utf8GraphQLParser.Parse(memoryStream.ToArray()));
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
        CancellationToken cancellationToken
        )
    {
        await using var outStream = await _blobContainerClient.GetBlobClient(documentId.Value)
            .OpenWriteAsync(overwrite: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        await document.WriteToAsync(outStream, cancellationToken).ConfigureAwait(false);
        await outStream.FlushAsync(cancellationToken);
    }
}

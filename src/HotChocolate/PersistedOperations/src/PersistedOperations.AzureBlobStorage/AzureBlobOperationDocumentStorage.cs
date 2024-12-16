using System.Buffers;
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
    private static readonly BlobOpenWriteOptions _writeOptions = new()
    {
        HttpHeaders = new BlobHttpHeaders
        {
            ContentType = "application/graphql",
            ContentDisposition = "inline",
            CacheControl = "public, max-age=604800, immutable"
        }
    };

    private readonly BlobContainerClient _client;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="client">The blob container client instance.</param>
    public AzureBlobOperationDocumentStorage(BlobContainerClient client)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        _client = client;
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
        CancellationToken ct)
    {
        var blobClient = _client.GetBlobClient(documentId.ToString());
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        var position = 0;

        try
        {
            await using var blobStream = await blobClient.OpenReadAsync(cancellationToken: ct).ConfigureAwait(false);
            while (true)
            {
                if (buffer.Length < position + 256)
                {
                    var newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = newBuffer;
                }

                var read = await blobStream.ReadAsync(buffer, position, 256, ct);
                position += read;

                if (read < 256)
                {
                    break;
                }
            }

            if (position == 0)
            {
                return null;
            }

            var span = new ReadOnlySpan<byte>(buffer, 0, position);
            return new OperationDocument(Utf8GraphQLParser.Parse(span));
        }
        catch (RequestFailedException e)
        {
            if (e.Status == 404)
            {
                return null;
            }

            throw;
        }
        finally
        {
            if(position > 0)
            {
                buffer.AsSpan().Slice(0, position).Clear();
            }

            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(
        OperationDocumentId documentId,
        IOperationDocument document,
        CancellationToken cancellationToken = default)
    {
        if(document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (OperationDocumentId.IsNullOrEmpty(documentId))
        {
            throw new ArgumentException(nameof(documentId));
        }

        return SaveInternalAsync(documentId, document, cancellationToken);
    }

    private async ValueTask SaveInternalAsync(
        OperationDocumentId documentId,
        IOperationDocument document,
        CancellationToken ct)
    {
        var blobClient = _client.GetBlobClient(documentId.ToString());
        await using var outStream = await blobClient.OpenWriteAsync(true, _writeOptions, ct).ConfigureAwait(false);
        await document.WriteToAsync(outStream, ct).ConfigureAwait(false);
        await outStream.FlushAsync(ct).ConfigureAwait(false);
    }
}

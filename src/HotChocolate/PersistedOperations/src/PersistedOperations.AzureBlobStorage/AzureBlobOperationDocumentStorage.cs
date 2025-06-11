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
    private static readonly char[] s_fileExtension = ".graphql".ToCharArray();

    private static readonly BlobOpenWriteOptions s_writeOptions = new()
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
        ArgumentNullException.ThrowIfNull(client);

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
        var blobClient = _client.GetBlobClient(CreateFileName(documentId));
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
                buffer.AsSpan()[..position].Clear();
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
        ArgumentNullException.ThrowIfNull(document);

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
        var blobClient = _client.GetBlobClient(CreateFileName(documentId));
        await using var outStream = await blobClient.OpenWriteAsync(true, s_writeOptions, ct).ConfigureAwait(false);
        await document.WriteToAsync(outStream, ct).ConfigureAwait(false);
        await outStream.FlushAsync(ct).ConfigureAwait(false);
    }

    private static string CreateFileName(OperationDocumentId documentId)
    {
        var length = documentId.Value.Length + s_fileExtension.Length;
        char[]? rented = null;
        Span<char> span = length <= 256
            ? stackalloc char[length]
            : rented = ArrayPool<char>.Shared.Rent(length);

        try
        {
            documentId.Value.AsSpan().CopyTo(span);
            s_fileExtension.AsSpan().CopyTo(span[documentId.Value.Length..]);
            return new string(span);
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }
}

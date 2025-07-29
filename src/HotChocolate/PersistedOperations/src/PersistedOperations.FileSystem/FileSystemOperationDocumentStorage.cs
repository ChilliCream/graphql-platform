using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.PersistedOperations.FileSystem;

/// <summary>
/// An implementation of <see cref="IOperationDocumentStorage"/> that uses the file system.
/// </summary>
public class FileSystemOperationDocumentStorage : IOperationDocumentStorage
{
    private readonly IOperationDocumentFileMap _documentMap;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="documentMap">The operation document identifier mapping.</param>
    public FileSystemOperationDocumentStorage(IOperationDocumentFileMap documentMap)
    {
        _documentMap = documentMap ?? throw new ArgumentNullException(nameof(documentMap));

        if (!Directory.Exists(_documentMap.Root))
        {
            Directory.CreateDirectory(documentMap.Root);
        }
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

        var filePath = _documentMap.MapToFilePath(documentId.Value);

        return File.Exists(filePath)
            ? TryReadInternalAsync(filePath, cancellationToken)
            : default;
    }

    private static async ValueTask<IOperationDocument?> TryReadInternalAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        const int chunkSize = 4096;
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var writer = new PooledArrayWriter();
        var read = 0;

        do
        {
            var memory = writer.GetMemory(chunkSize);
            read = await stream.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
            writer.Advance(read);
        } while (read == chunkSize);

        var document = Utf8GraphQLParser.Parse(writer.WrittenSpan);
        return new OperationDocument(document);
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(
        OperationDocumentId documentId,
        IOperationDocument document,
        CancellationToken cancellationToken = default)
    {
        if (OperationDocumentId.IsNullOrEmpty(documentId))
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        ArgumentNullException.ThrowIfNull(document);

        var filePath = _documentMap.MapToFilePath(documentId.Value);
        return SaveInternalAsync(filePath, document, cancellationToken);
    }

    private static async ValueTask SaveInternalAsync(
        string filePath,
        IOperationDocument document,
        CancellationToken cancellationToken)
    {
        if (File.Exists(filePath))
        {
            return;
        }

        await using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
        await document.WriteToAsync(stream, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}

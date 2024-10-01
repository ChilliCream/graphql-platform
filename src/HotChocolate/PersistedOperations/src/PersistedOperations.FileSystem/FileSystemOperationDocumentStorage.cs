using HotChocolate.Execution;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.PersistedOperations.FileSystem;

/// <summary>
/// An implementation of <see cref="IOperationDocumentStorage"/> that uses the file system.
/// </summary>
public class FileSystemOperationDocumentStorage : IOperationDocumentStorage
{
    private static readonly Task<OperationDocument?> _null = Task.FromResult<OperationDocument?>(null);
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
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        var document = await BufferHelper.ReadAsync(
                stream,
                static (buffer, buffered) =>
                {
                    var span = buffer.AsSpan().Slice(0, buffered);
                    return Utf8GraphQLParser.Parse(span);
                },
                cancellationToken)
            .ConfigureAwait(false);

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

        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

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

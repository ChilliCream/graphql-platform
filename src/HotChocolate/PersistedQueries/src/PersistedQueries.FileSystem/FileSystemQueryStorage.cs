using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.PersistedQueries.FileSystem;

/// <summary>
/// An implementation of <see cref="IOperationDocumentStorage"/> that uses the file system.
/// </summary>
public class FileSystemQueryStorage : IOperationDocumentStorage
{
    private static readonly Task<OperationDocument?> _null = Task.FromResult<OperationDocument?>(null);
    private readonly IQueryFileMap _queryMap;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="queryMap">The query identifier mapping.</param>
    public FileSystemQueryStorage(IQueryFileMap queryMap)
    {
        _queryMap = queryMap ?? throw new ArgumentNullException(nameof(queryMap));

        if (!Directory.Exists(_queryMap.Root))
        {
            Directory.CreateDirectory(queryMap.Root);
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

        var filePath = _queryMap.MapToFilePath(documentId.Value);

        return File.Exists(filePath)
            ? TryReadInternalAsync(filePath, cancellationToken)
            : default;
    }

    private static async ValueTask<IOperationDocument?> TryReadInternalAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
#else
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
#endif

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

        var filePath = _queryMap.MapToFilePath(documentId.Value);
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

#if NETSTANDARD2_0
        using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
#else
        await using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
#endif
        await document.WriteToAsync(stream, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
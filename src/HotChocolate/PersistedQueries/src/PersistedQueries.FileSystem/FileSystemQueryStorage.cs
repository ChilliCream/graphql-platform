using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.PersistedQueries.FileSystem;

/// <summary>
/// An implementation of <see cref="IReadStoredQueries"/>
/// and <see cref="IWriteStoredQueries"/> that
/// uses the local file system.
/// </summary>
public class FileSystemQueryStorage
    : IReadStoredQueries
    , IWriteStoredQueries
{
    private static readonly Task<OperationDocument?> _null = Task.FromResult<OperationDocument?>(null);
    private readonly IQueryFileMap _queryMap;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="queryMap">The query identifier mapping.</param>
    public FileSystemQueryStorage(IQueryFileMap queryMap)
    {
        _queryMap = queryMap
            ?? throw new ArgumentNullException(nameof(queryMap));

        if (!Directory.Exists(_queryMap.Root))
        {
            Directory.CreateDirectory(queryMap.Root);
        }
    }

    /// <inheritdoc />
    public Task<OperationDocument?> TryReadQueryAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        var filePath = _queryMap.MapToFilePath(documentId);

        if (!File.Exists(filePath))
        {
            return _null;
        }

        return TryReadQueryInternalAsync(filePath, cancellationToken);
    }

    private static async Task<OperationDocument?> TryReadQueryInternalAsync(
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
    public Task WriteQueryAsync(
        string queryId,
        IOperationDocument document,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queryId))
        {
            throw new ArgumentNullException(nameof(queryId));
        }

        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var filePath = _queryMap.MapToFilePath(queryId);
        return WriteQueryInternalAsync(document, filePath, cancellationToken);
    }

    private static async Task WriteQueryInternalAsync(
        IOperationDocument document,
        string filePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
            await document.WriteToAsync(stream, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

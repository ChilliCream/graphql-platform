using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.PersistedQueries.FileSystem
{
    /// <summary>
    /// An implementation of <see cref="IReadStoredQueries"/>
    /// and <see cref="IWriteStoredQueries"/> that
    /// uses the local file system.
    /// </summary>
    public class FileSystemQueryStorage
        : IReadStoredQueries
        , IWriteStoredQueries
    {
        private static readonly Task<QueryDocument?> _null = Task.FromResult<QueryDocument?>(null);
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
        public Task<QueryDocument?> TryReadQueryAsync(
            string queryId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queryId))
            {
                throw new ArgumentNullException(nameof(queryId));
            }

            var filePath = _queryMap.MapToFilePath(queryId);

            if (!File.Exists(filePath))
            {
                return _null;
            }

            return TryReadQueryInternalAsync(queryId, filePath, cancellationToken);
        }

        private async Task<QueryDocument?> TryReadQueryInternalAsync(
            string queryId,
            string filePath,
            CancellationToken cancellationToken)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            DocumentNode document = await BufferHelper.ReadAsync(
                stream,
                (buffer, buffered) =>
                {
                    Span<byte> span = buffer.AsSpan().Slice(0, buffered);
                    return Utf8GraphQLParser.Parse(span);
                },
                cancellationToken)
                .ConfigureAwait(false);

            return new QueryDocument(document);
        }

        /// <inheritdoc />
        public Task WriteQueryAsync(
            string queryId,
            IQuery query,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queryId))
            {
                throw new ArgumentNullException(nameof(queryId));
            }

            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var filePath = _queryMap.MapToFilePath(queryId);
            return WriteQueryInternalAsync(queryId, query, filePath, cancellationToken);
        }

        private async Task WriteQueryInternalAsync(
            string queryId,
            IQuery query,
            string filePath,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
            {
                using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
                await query.WriteToAsync(stream, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

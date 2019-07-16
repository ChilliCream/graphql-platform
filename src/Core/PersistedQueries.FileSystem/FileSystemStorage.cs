using HotChocolate.Execution;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HotChocolate.PersistedQueries.FileSystem
{
    /// <summary>
    /// An implementation of <see cref="IReadStoredQueries"/> that
    /// uses the local file system as a storage medium.
    /// </summary>
    public class FileSystemStorage : IReadStoredQueries
    {
        private readonly IQueryFileMap _queryMap;
        private readonly IQueryRequestBuilder _requestBuilder;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="queryMap">The query identifier mapping.</param>
        /// <param name="requestBuilder">The request builder.</param>
        public FileSystemStorage(IQueryFileMap queryMap,
            IQueryRequestBuilder requestBuilder)
        {
            _queryMap = queryMap ?? throw new ArgumentNullException(nameof(queryMap));
            _requestBuilder = requestBuilder ?? throw new ArgumentNullException(nameof(requestBuilder));
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">
        /// Thrown when <see cref="queryId"/> is null or white space.
        /// </exception>
        /// <exception cref="QueryNotFoundException">
        /// Thrown when query Id maps to a file that does not exist, or maps
        /// to null or white space.
        /// </exception>
        public async Task<IQuery> ReadQueryAsync(string queryId)
        {
            if (string.IsNullOrWhiteSpace(queryId))
            {
                throw new ArgumentNullException(nameof(queryId));
            }

            var filePath = _queryMap.MapToFilePath(queryId);

            if (!File.Exists(filePath))
            {
                throw new QueryNotFoundException(queryId);
            }

            using (var fs = new FileStream(filePath, FileMode.Open))
            {
                using (var s = new StreamReader(fs))
                {
                    var fileContent = await s.ReadToEndAsync();
                    var readonlyRequest = _requestBuilder
                        .SetQuery(fileContent)
                        .Create();

                    return readonlyRequest.Query;
                }
            }
        }
    }
}

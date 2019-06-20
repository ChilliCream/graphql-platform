using HotChocolate.Execution;
using System;
using System.IO;
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

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="queryMap">The query identifier mapping.</param>
        public FileSystemStorage(IQueryFileMap queryMap)
        {
            _queryMap = queryMap;
        }
        
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">
        /// Thrown when <see cref="queryIdentifier"/> is null or white space.
        /// </exception>
        /// <exception cref="QueryNotFoundException">
        /// Thrown when <see cref="queryIdentifier"/> does not map to a file
        /// that exists.
        /// </exception>
        public async Task<IQuery> ReadQueryAsync(string queryIdentifier)
        {
            if (string.IsNullOrWhiteSpace(queryIdentifier))
            {
                throw new ArgumentNullException(queryIdentifier);
            }

            var filePath = _queryMap.MapToFilePath(queryIdentifier);

            if (!File.Exists(filePath))
            {
                throw new QueryNotFoundException(queryIdentifier);
            }

            using (var fs = new FileStream(filePath, FileMode.Open))
            {
                using (var s = new StreamReader(fs))
                {
                    var fileContent = await s.ReadToEndAsync();
                    
                    // TODO: Parse into IQuery and return.
                    // TODO: Possible optimization of storing in memory for faster access.
                }
            }
            
            throw new NotImplementedException();
        }
    }
}

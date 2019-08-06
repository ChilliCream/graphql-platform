using System;
using System.Buffers;
using IOPath = System.IO.Path;

namespace HotChocolate.PersistedQueries.FileSystem
{
    /// <summary>
    /// A default implementation of <see cref="IQueryFileMap"/>.
    /// </summary>
    public class DefaultQueryFileMap : IQueryFileMap
    {
        private const int _maxStackSize = 256;
        private readonly string _cacheDirectory;
        private const char _forwardSlash = '/';
        private const char _dash = '-';
        private const char _plus = '+';
        private const char _underscore = '_';
        private const char _equals = '=';

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public DefaultQueryFileMap()
            : this("persisted_queries")
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public DefaultQueryFileMap(string cacheDirectory)
        {
            _cacheDirectory = cacheDirectory;
        }

        /// <inheritdoc />
        public string Root => _cacheDirectory;

        /// <inheritdoc />
        public string MapToFilePath(string queryId)
        {
            if (string.IsNullOrEmpty(queryId))
            {
                throw new ArgumentNullException(nameof(queryId));
            }

            string fileName = EncodeQueryId(queryId);

            if (_cacheDirectory != null)
            {
                fileName = IOPath.Combine(_cacheDirectory, fileName);
            }

            return fileName;
        }

        private static unsafe string EncodeQueryId(string queryId)
        {
            char[] encodedBuffer = null;

            Span<char> encoded = queryId.Length <= _maxStackSize
                ? stackalloc char[queryId.Length]
                : (encodedBuffer = ArrayPool<char>.Shared.Rent(queryId.Length));

            try
            {
                for (var i = 0; i < encoded.Length; i++)
                {
                    switch (queryId[i])
                    {
                        case _forwardSlash:
                            encoded[i] = _dash;
                            break;

                        case _plus:
                            encoded[i] = _underscore;
                            break;

                        case _equals:
                            encoded = encoded.Slice(0, i);
                            break;

                        default:
                            encoded[i] = queryId[i];
                            break;
                    }
                }

                fixed (char* charPtr = encoded)
                {
                    return new string(charPtr, 0, encoded.Length);
                }
            }
            finally
            {
                if (encodedBuffer != null)
                {
                    encoded.Clear();
                    ArrayPool<char>.Shared.Return(encodedBuffer);
                }
            }
        }
    }
}

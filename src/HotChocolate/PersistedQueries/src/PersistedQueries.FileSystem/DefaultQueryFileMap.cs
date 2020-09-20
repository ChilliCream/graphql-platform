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
        private const int _maxStackSize = 128;
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
            _cacheDirectory = cacheDirectory ??
                throw new ArgumentNullException(nameof(cacheDirectory));
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

            return IOPath.Combine(_cacheDirectory, EncodeQueryId(queryId));
        }

        private static unsafe string EncodeQueryId(string queryId)
        {
            char[]? encodedBuffer = null;
            var queryIdLength = queryId.Length + 8;

            Span<char> encoded = queryIdLength <= _maxStackSize
                ? stackalloc char[queryIdLength]
                : (encodedBuffer = ArrayPool<char>.Shared.Rent(queryIdLength));

            try
            {
                var i = 0;

                for (; i < queryId.Length; i++)
                {
                    if (queryId[i] == _forwardSlash)
                    {
                        encoded[i] = _dash;
                    }
                    else if (queryId[i] == _plus)
                    {
                        encoded[i] = _underscore;
                    }
                    else if (queryId[i] == _equals)
                    {
                        break;
                    }
                    else
                    {
                        encoded[i] = queryId[i];
                    }
                }

                encoded[i++] = '.';
                encoded[i++] = 'g';
                encoded[i++] = 'r';
                encoded[i++] = 'a';
                encoded[i++] = 'p';
                encoded[i++] = 'h';
                encoded[i++] = 'q';
                encoded[i++] = 'l';

                encoded = encoded.Slice(0, i);

                fixed (char* charPtr = encoded)
                {
                    return new string(charPtr, 0, i);
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

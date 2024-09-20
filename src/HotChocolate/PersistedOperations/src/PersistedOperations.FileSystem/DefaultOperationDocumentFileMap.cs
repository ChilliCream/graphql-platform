using System.Buffers;
using IOPath = System.IO.Path;

namespace HotChocolate.PersistedOperations.FileSystem;

/// <summary>
/// A default implementation of <see cref="IOperationDocumentFileMap"/>.
/// </summary>
public class DefaultOperationDocumentFileMap : IOperationDocumentFileMap
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
    public DefaultOperationDocumentFileMap()
        : this("persisted_operations")
    {
    }

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    public DefaultOperationDocumentFileMap(string cacheDirectory)
    {
        _cacheDirectory = cacheDirectory ??
            throw new ArgumentNullException(nameof(cacheDirectory));
    }

    /// <inheritdoc />
    public string Root => _cacheDirectory;

    /// <inheritdoc />
    public string MapToFilePath(string documentId)
    {
        if (string.IsNullOrEmpty(documentId))
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        return IOPath.Combine(_cacheDirectory, EncodeDocumentId(documentId));
    }

    private static unsafe string EncodeDocumentId(string documentId)
    {
        char[]? encodedBuffer = null;
        var documentIdLength = documentId.Length + 8;

        var encoded = documentIdLength <= _maxStackSize
            ? stackalloc char[documentIdLength]
            : encodedBuffer = ArrayPool<char>.Shared.Rent(documentIdLength);

        try
        {
            var i = 0;

            for (; i < documentId.Length; i++)
            {
                if (documentId[i] == _forwardSlash)
                {
                    encoded[i] = _dash;
                }
                else if (documentId[i] == _plus)
                {
                    encoded[i] = _underscore;
                }
                else if (documentId[i] == _equals)
                {
                    break;
                }
                else
                {
                    encoded[i] = documentId[i];
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

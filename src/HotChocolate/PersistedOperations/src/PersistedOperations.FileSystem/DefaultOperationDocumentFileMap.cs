using System.Buffers;
using IOPath = System.IO.Path;

namespace HotChocolate.PersistedOperations.FileSystem;

/// <summary>
/// A default implementation of <see cref="IOperationDocumentFileMap"/>.
/// </summary>
public class DefaultOperationDocumentFileMap : IOperationDocumentFileMap
{
    private const int MaxStackSize = 128;
    private readonly string _cacheDirectory;
    private const char ForwardSlash = '/';
    private const char Dash = '-';
    private const char Plus = '+';
    private const char Underscore = '_';
    private const char EqualsSign = '=';

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
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        return IOPath.Combine(_cacheDirectory, EncodeDocumentId(documentId));
    }

    private static unsafe string EncodeDocumentId(string documentId)
    {
        char[]? encodedBuffer = null;
        var documentIdLength = documentId.Length + 8;

        var encoded = documentIdLength <= MaxStackSize
            ? stackalloc char[documentIdLength]
            : encodedBuffer = ArrayPool<char>.Shared.Rent(documentIdLength);

        try
        {
            var i = 0;

            for (; i < documentId.Length; i++)
            {
                if (documentId[i] == ForwardSlash)
                {
                    encoded[i] = Dash;
                }
                else if (documentId[i] == Plus)
                {
                    encoded[i] = Underscore;
                }
                else if (documentId[i] == EqualsSign)
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

            encoded = encoded[..i];

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

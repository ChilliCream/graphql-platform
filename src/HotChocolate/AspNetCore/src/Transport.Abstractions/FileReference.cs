using static HotChocolate.Transport.Properties.TransportAbstractionResources;

namespace HotChocolate.Transport.Http;

/// <summary>
/// A file reference can be used to upload a file with the
/// GraphQL multipart request protocol.
/// </summary>
public sealed class FileReference
{
    private readonly Func<Stream> _openRead;

    /// <inheritdoc cref="FileReference(Stream,string,string)"/>
    public FileReference(Stream stream, string fileName)
        : this(stream, fileName, null)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="FileReference" />
    /// </summary>
    /// <param name="stream">
    /// The file stream.
    /// </param>
    /// <param name="fileName">
    /// The file name.
    /// </param>
    /// <param name="contentType">
    /// The file content type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="stream"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fileName"/> is <c>null</c>, empty or white space.
    /// </exception>
    public FileReference(Stream stream, string fileName, string? contentType)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                FileReference_FileName_NullOrEmpty,
                nameof(fileName));
        }

        _openRead = () => stream;
        FileName = fileName;
        ContentType = contentType;
    }

    /// <inheritdoc cref="FileReference(Func{Stream},string,string)"/>
    public FileReference(Func<Stream> openRead, string fileName)
        : this(openRead, fileName, null)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="FileReference" />
    /// </summary>
    /// <param name="openRead">
    /// The stream factory.
    /// </param>
    /// <param name="fileName">
    /// The file name.
    /// </param>
    /// <param name="contentType">
    /// The file content type.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="fileName"/> is <c>null</c>, empty or white space.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="openRead"/> is <c>null</c>.
    /// </exception>
    public FileReference(Func<Stream> openRead, string fileName, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                FileReference_FileName_NullOrEmpty,
                nameof(fileName));
        }

        _openRead = openRead ?? throw new ArgumentNullException(nameof(openRead));
        FileName = fileName;
        ContentType = contentType;
    }

    /// <summary>
    /// The file name e.g. <c>"foo.txt"</c>.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// The content type of the file e.g. <c>"application/pdf"</c>.
    /// </summary>
    public string? ContentType { get; }

    /// <summary>
    /// Opens the file stream.
    /// </summary>
    /// <returns>
    /// Returns the file stream.
    /// </returns>
    public Stream OpenRead() => _openRead();
}

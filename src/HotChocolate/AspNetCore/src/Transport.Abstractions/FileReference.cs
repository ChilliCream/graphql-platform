using static HotChocolate.Transport.Properties.TransportAbstractionResources;

namespace HotChocolate.Transport.Http;

/// <summary>
/// A file reference can be used to upload a file with the
/// GraphQL multipart request protocol.
/// </summary>
public sealed class FileReference
{
    private readonly Func<Stream> _openRead;

    /// <summary>
    /// Creates a new instance of <see cref="FileReference" />
    /// </summary>
    /// <param name="stream">
    /// The file stream.
    /// </param>
    /// <param name="fileName">
    /// The file name.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="stream"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fileName"/> is <c>null</c>, empty or white space.
    /// </exception>
    public FileReference(Stream stream, string fileName)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                FileReference_FileName_NullOrEmpty,
                nameof(fileName));
        }

        _openRead = () => stream;
        FileName = fileName;
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
    /// <exception cref="ArgumentException">
    /// <paramref name="fileName"/> is <c>null</c>, empty or white space.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="openRead"/> is <c>null</c>.
    /// </exception>
    public FileReference(Func<Stream> openRead, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                FileReference_FileName_NullOrEmpty,
                nameof(fileName));
        }

        _openRead = openRead ?? throw new ArgumentNullException(nameof(openRead));
        FileName = fileName;
    }

    /// <summary>
    /// The file name eg. <c>foo.txt</c>.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Opens the file stream.
    /// </summary>
    /// <returns>
    /// Returns the file stream.
    /// </returns>
    public Stream OpenRead() => _openRead();
}

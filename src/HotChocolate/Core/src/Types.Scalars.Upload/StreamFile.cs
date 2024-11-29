using HotChocolate.Types.Properties;

namespace HotChocolate.Types;

/// <summary>
/// An implementation of <see cref="IFile"/> that allows to pass in streams into the
/// execution engine.
/// </summary>
public class StreamFile : IFile
{
    private readonly Func<Stream> _openReadStream;

    /// <summary>
    /// Creates a new instance of <see cref="StreamFile"/>.
    /// </summary>
    /// <param name="name">
    /// The file name.
    /// </param>
    /// <param name="openReadStream">
    /// A delegate to open the stream.
    /// </param>
    /// <param name="length">
    /// The file length if available.
    /// </param>
    /// <param name="contentType">
    /// The file content-type.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="openReadStream"/> is <c>null</c>.
    /// </exception>
    public StreamFile(
        string name,
        Func<Stream> openReadStream,
        long? length = null,
        string? contentType = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(
                UploadResources.StreamFile_Constructor_NameCannotBeNullOrEmpty,
                nameof(name));
        }

        Name = name;
        _openReadStream = openReadStream ??
                          throw new ArgumentNullException(nameof(openReadStream));
        Length = length;
        ContentType = contentType;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public long? Length { get; }

    /// <inheritdoc />
    public string? ContentType { get; }

    /// <inheritdoc />
    public virtual async Task CopyToAsync(
        Stream target,
        CancellationToken cancellationToken = default)
    {
        await using var stream = OpenReadStream();

        await stream.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual Stream OpenReadStream() => _openReadStream();
}

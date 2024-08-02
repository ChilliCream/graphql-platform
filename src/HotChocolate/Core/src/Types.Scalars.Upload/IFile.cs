namespace HotChocolate.Types;

/// <summary>
/// Represents a uploaded file.
/// </summary>
public interface IFile
{
    /// <summary>
    /// Gets the file name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the file length in bytes.
    /// </summary>
    long? Length { get; }

    /// <summary>
    /// Gets the content type of the file if it is available.
    /// </summary>
    string? ContentType { get; }

    /// <summary>
    /// Asynchronously copies the contents of the uploaded file to the target stream.
    /// </summary>
    /// <param name="target">
    /// The stream to copy the file contents to.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    Task CopyToAsync(Stream target, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the request stream for reading the uploaded file.
    /// </summary>
    Stream OpenReadStream();
}

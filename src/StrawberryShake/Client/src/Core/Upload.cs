namespace StrawberryShake;

/// <summary>
/// Wrapper to upload a file to a GraphQL Server
/// </summary>
public readonly struct Upload
{
    /// <inheritdoc cref="Upload(Stream, string, string?)"/>
    public Upload(Stream content, string fileName)
        : this(content, fileName, null)
    {
    }

    /// <summary>
    /// Creates a new instance of the Upload-scalar.
    /// </summary>
    public Upload(Stream content, string fileName, string? contentType)
    {
        Content = content;
        FileName = fileName;
        ContentType = contentType;
    }

    /// <summary>
    /// The content that is streamed to the server
    /// </summary>
    public Stream Content { get; }

    /// <summary>
    /// The name of the file
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// The optional MIME type of the file.
    /// </summary>
    /// <remarks>
    /// If specified, this value is applied as the HTTP Content-Type header.
    /// </remarks>
    public string? ContentType { get; }
}

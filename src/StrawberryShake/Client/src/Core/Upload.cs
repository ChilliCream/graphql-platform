namespace StrawberryShake;

/// <summary>
/// Wrapper to upload a file to a GraphQL Server
/// </summary>
public readonly struct Upload
{
    /// <summary>
    /// Creates a new instance of Upload
    /// </summary>
    public Upload(Stream content, string fileName)
    {
        Content = content;
        FileName = fileName;
    }

    /// <summary>
    /// The content that is streamed to the server
    /// </summary>
    public Stream Content { get; }

    /// <summary>
    /// The name of the file
    /// </summary>
    public string FileName { get; }
}

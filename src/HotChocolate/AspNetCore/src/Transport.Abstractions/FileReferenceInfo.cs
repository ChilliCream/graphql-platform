namespace HotChocolate.Transport.Http;

/// <summary>
/// The file reference info contains the actual
/// <see cref="FileReference"/> and the reference
/// name that is used to upload the referenced file.
/// </summary>
public sealed class FileReferenceInfo
{
    /// <summary>
    /// Creates a new instance of <see cref="FileReferenceInfo" />
    /// </summary>
    /// <param name="file">
    /// The file reference.
    /// </param>
    /// <param name="name">
    /// The internal reference name.
    /// </param>
    internal FileReferenceInfo(FileReference file, string name)
    {
        File = file;
        Name = name;
    }

    /// <summary>
    /// Gets the file that shall be uploaded.
    /// </summary>
    public FileReference File { get; }

    /// <summary>
    /// Gets the internal reference name that is used to refer to
    /// this file instance within the GraphQL multipart request protocol.
    /// </summary>
    public string Name { get; }
}

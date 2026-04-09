using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// Represents an upload value node in the GraphQL abstract syntax tree (AST).
/// This value node is used to represent file uploads in GraphQL operations,
/// containing both a key for identification and the actual file data.
/// </summary>
public sealed class UploadValueNode : IValueNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UploadValueNode"/> class.
    /// </summary>
    /// <param name="key">
    /// The unique key identifying this upload in the multipart request.
    /// </param>
    /// <param name="file">
    /// The file data associated with this upload.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is <c>null</c> or empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="file"/> is <c>null</c>.
    /// </exception>
    public UploadValueNode(string key, IFile file)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(file);

        Key = key;
        File = file;
    }

    /// <summary>
    /// Gets the unique key identifying this upload in the multipart request.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the file data associated with this upload.
    /// </summary>
    public IFile File { get; }

    object? IValueNode.Value => Key;

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.StringValue;

    /// <inheritdoc />
    public Language.Location? Location => null;

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes() => [];

    /// <inheritdoc />
    public override string ToString() => ToString(true);

    /// <inheritdoc />
    public string ToString(bool indented) => $"\"{Key}\"";
}

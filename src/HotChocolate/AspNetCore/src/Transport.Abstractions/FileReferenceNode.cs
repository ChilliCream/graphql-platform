using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Transport.Http;

/// <summary>
/// This file literal is used in order to allow for file references in <see cref="ObjectValueNode"/>.
/// </summary>
public sealed class FileReferenceNode
    : IValueNode<FileReference>
    , IValueNode<string>
    , IEquatable<FileReferenceNode>
{
    /// <summary>
    /// Creates a new instance of <see cref="FileReferenceNode" />
    /// </summary>
    /// <param name="stream">
    /// The file stream.
    /// </param>
    /// <param name="fileName">
    /// The file name.
    /// </param>
    public FileReferenceNode(Stream stream, string fileName)
        : this(new FileReference(() => stream, fileName)) { }

    /// <summary>
    /// Creates a new instance of <see cref="FileReferenceNode" />
    /// </summary>
    /// <param name="openRead">
    /// The stream factory.
    /// </param>
    /// <param name="fileName">
    /// The file name.
    /// </param>
    public FileReferenceNode(Func<Stream> openRead, string fileName)
        : this(new FileReference(openRead, fileName)) { }

    /// <summary>
    /// Creates a new instance of <see cref="FileReferenceNode" />
    /// </summary>
    /// <param name="value">
    /// The file Reference.
    /// </param>
    public FileReferenceNode(FileReference value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Returns the <see cref="SyntaxKind"/> of the node.
    /// </summary>
    public SyntaxKind Kind => SyntaxKind.StringValue;

    /// <summary>
    /// Gets a <see cref="Location"/> of this node in the parsed source text
    /// if available the parser provided this information.
    /// </summary>
    public Location? Location => null;

    /// <summary>
    /// Gets the actual file reference.
    /// </summary>
    public FileReference Value { get; }

    object? IValueNode.Value => Value;

    string IValueNode<string>.Value => Value.FileName;

    /// <summary>
    /// Gets the children of this node.
    /// </summary>
    /// <returns>
    /// Returns the children of this node..
    /// </returns>
    public IEnumerable<ISyntaxNode> GetNodes() => [];

    /// <summary>
    /// Determines whether the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="FileReferenceNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="IValueNode"/> to compare with the current
    /// <see cref="FileReferenceNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="FileReferenceNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(FileReferenceNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other) ||
            ReferenceEquals(Value, other.Value))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="FileReferenceNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="IValueNode"/> to compare with the current
    /// <see cref="FileReferenceNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="FileReferenceNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(IValueNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is FileReferenceNode file)
        {
            return Equals(file);
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to
    /// the current <see cref="FileReferenceNode"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current
    /// <see cref="FileReferenceNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to the
    /// current <see cref="FileReferenceNode"/>; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(obj, this))
        {
            return true;
        }

        return Equals(obj as FileReferenceNode);
    }

    /// <summary>
    /// Serves as a hash function for a <see cref="FileReferenceNode"/>
    /// object.
    /// </summary>
    /// <returns>
    /// A hash code for this instance that is suitable for use in
    /// hashing algorithms and data structures such as a hash table.
    /// </returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return (Kind.GetHashCode() * 397) ^ (Value.GetHashCode() * 97);
        }
    }

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString()
        => ToString(true);

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <param name="indented">
    /// A value that indicates whether the GraphQL output should be formatted,
    /// which includes indenting nested GraphQL tokens, adding
    /// new lines, and adding white space between property names and values.
    /// </param>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public string ToString(bool indented)
        => SyntaxPrinter.Print(this, indented);
}

using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Types;

/// <summary>
/// This file literal is used in order to allow for
/// an optimized path through the execution engine.
/// </summary>
public class FileValueNode
    : IValueNode<IFile>
    , IValueNode<string>
    , IEquatable<FileValueNode>
{
    /// <summary>
    /// Creates a new instance of <see cref="FileValueNode" />
    /// </summary>
    /// <param name="file">
    /// The file.
    /// </param>
    public FileValueNode(IFile file)
    {
        Value = file ?? throw new ArgumentNullException(nameof(file));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.StringValue;

    /// <inheritdoc />
    public Language.Location? Location => null;

    /// <inheritdoc />
    public IFile Value { get; }

    /// <inheritdoc />
    object IValueNode.Value => Value;

    string IValueNode<string>.Value => Value.Name;

    /// <summary>
    /// Determines whether the specified <see cref="FileValueNode"/>
    /// is equal to the current <see cref="FileValueNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="FileValueNode"/> to compare with the current
    /// <see cref="FileValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="FileValueNode"/> is equal
    /// to the current <see cref="FileValueNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(FileValueNode? other)
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
    /// to the current <see cref="FileValueNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="IValueNode"/> to compare with the current
    /// <see cref="FileValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="FileValueNode"/>;
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

        if (other is FileValueNode file)
        {
            return Equals(file);
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to
    /// the current <see cref="FileValueNode"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current
    /// <see cref="FileValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to the
    /// current <see cref="FileValueNode"/>; otherwise, <c>false</c>.
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

        return Equals(obj as FileValueNode);
    }

    public IEnumerable<ISyntaxNode> GetNodes() => [];

    /// <summary>
    /// Serves as a hash function for a <see cref="FileValueNode"/>
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
            return (Kind.GetHashCode() * 397)
             ^ (Value.GetHashCode() * 97);
        }
    }

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString() => ToString(true);

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
    public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);
}

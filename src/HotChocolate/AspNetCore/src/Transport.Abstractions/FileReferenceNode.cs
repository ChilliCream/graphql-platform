using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Transport.Http;

/// <summary>
/// This file literal is used in order to allow for
/// an optimized path through the execution engine.
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

    public SyntaxKind Kind => SyntaxKind.StringValue;

    public Location? Location => null;

    public FileReference Value { get; }

    object? IValueNode.Value => Value;

    string IValueNode<string>.Value => Value.FileName;

    public IEnumerable<ISyntaxNode> GetNodes()
        => Enumerable.Empty<ISyntaxNode>();

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

    public override string ToString()
        => ToString(true);

    public string ToString(bool indented)
        => SyntaxPrinter.Print(this, indented);
}
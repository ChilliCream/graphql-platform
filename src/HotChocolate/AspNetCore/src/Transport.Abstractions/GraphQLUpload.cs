using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Transport.Http;

public class FileUploadNode 
    : IValueNode<FileUpload>
    , IValueNode<string>
    , IEquatable<FileUploadNode>
{
    public FileUploadNode(FileUpload value)
    {
        Value = value;
    }

    public SyntaxKind Kind => SyntaxKind.StringValue;

    public Location? Location => null;
    
    public FileUpload Value { get; }

    object? IValueNode.Value => Value;
    
    string IValueNode<string>.Value => Value.FileName;

    public IEnumerable<ISyntaxNode> GetNodes()
        => Enumerable.Empty<ISyntaxNode>();

    /// <summary>
    /// Determines whether the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="FileUploadNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="IValueNode"/> to compare with the current
    /// <see cref="FileUploadNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="FileUploadNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(FileUploadNode other)
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
    /// to the current <see cref="FileUploadNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="IValueNode"/> to compare with the current
    /// <see cref="FileUploadNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="FileUploadNode"/>;
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

        if (other is FileUploadNode file)
        {
            return Equals(file);
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to
    /// the current <see cref="FileUploadNode"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current
    /// <see cref="FileUploadNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to the
    /// current <see cref="FileUploadNode"/>; otherwise, <c>false</c>.
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

        return Equals(obj as FileUploadNode);
    }
    
    /// <summary>
    /// Serves as a hash function for a <see cref="FileUploadNode"/>
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

    public override string ToString() 
        => ToString(true);

    public string ToString(bool indented)
        => SyntaxPrinter.Print(this, indented);
}

public sealed class FileUpload
{
    private readonly Func<Stream> _openRead;

    public FileUpload(Func<Stream> openRead, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
        }
        
        _openRead = openRead ?? throw new ArgumentNullException(nameof(openRead));
        FileName = fileName;
    }

    public string FileName { get; }
    
    public Stream OpenRead() => _openRead();
}

public sealed class FileUploadInfo
{
    internal FileUploadInfo(FileUpload file, string name)
    {
        Name = name;
        File = file;
    }

    public string Name { get; }
    
    public FileUpload File { get; }
}
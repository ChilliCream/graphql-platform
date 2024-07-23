using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represents the null value syntax.
/// </summary>
public sealed class NullValueNode
    : IValueNode<object?>
    , IEquatable<NullValueNode>
{
    private NullValueNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NullValueNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    public NullValueNode(Location? location)
    {
        Location = location;
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.NullValue;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// The null runtime value.
    /// </summary>
    public object? Value { get; }

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes() => [];

    /// <summary>
    /// Determines whether the specified <see cref="NullValueNode"/>
    /// is equal to the current <see cref="NullValueNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="NullValueNode"/> to compare with the current
    /// <see cref="NullValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="NullValueNode"/> is equal
    /// to the current <see cref="NullValueNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(NullValueNode? other) => other is not null;

    /// <summary>
    /// Determines whether the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="NullValueNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="IValueNode"/> to compare with the current
    /// <see cref="NullValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="NullValueNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(IValueNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(other, this))
        {
            return true;
        }

        return other is NullValueNode;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to
    /// the current <see cref="NullValueNode"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current
    /// <see cref="NullValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to the
    /// current <see cref="NullValueNode"/>; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(obj, this))
        {
            return true;
        }

        return obj is NullValueNode;
    }

    /// <summary>
    /// Serves as a hash function for a <see cref="NullValueNode"/>
    /// object.
    /// </summary>
    /// <returns>
    /// A hash code for this instance that is suitable for use in
    /// hashing algorithms and data structures such as a hash table.
    /// </returns>
    public override int GetHashCode() => HashCode.Combine(Kind);

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString() => SyntaxPrinter.Print(this, true);

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

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Location" /> with <paramref name="location" />.
    /// </summary>
    /// <param name="location">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="location" />.
    /// </returns>
    public NullValueNode WithLocation(Location? location) => new(location);

    /// <summary>
    /// Gets the default null value instance.
    /// </summary>
    public static NullValueNode Default { get; } = new();
}

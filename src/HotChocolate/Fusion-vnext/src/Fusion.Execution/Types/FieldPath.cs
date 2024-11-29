using System.Text;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents GraphQL selection path which is used in the operation compiler.
/// </summary>
public sealed class FieldPath : IEquatable<FieldPath>
{
    private FieldPath(string name, FieldPath? parent = null)
    {
        Name = name;
        Parent = parent;
    }

    /// <summary>
    /// Gets the name of the current path segment.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the parent path segment.
    /// </summary>
    public FieldPath? Parent { get; }

    /// <summary>
    /// Gets the root path segment.
    /// </summary>
    public static FieldPath Root { get; } = new("$root");

    /// <summary>
    /// Creates a new path segment.
    /// </summary>
    /// <param name="name">
    /// The name of the path segment.
    /// </param>
    /// <returns>
    /// Returns a new path segment.
    /// </returns>
    public FieldPath Append(string name) => new(name, this);

    /// <summary>
    /// Indicates whether the current path is equal to another path.
    /// </summary>
    /// <param name="other">A path to compare with this path.</param>
    /// <returns>
    /// <see langword="true" /> if the current path is equal to the
    /// <paramref name="other" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(FieldPath? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Name.Equals(other.Name, StringComparison.Ordinal))
        {
            if (ReferenceEquals(Parent, other.Parent))
            {
                return true;
            }

            if (ReferenceEquals(Parent, null))
            {
                return false;
            }

            return Parent.Equals(other.Parent);
        }

        return false;
    }

    /// <summary>
    /// Indicates whether the current path is equal to another path.
    /// </summary>
    /// <param name="obj">
    /// An object to compare with this path.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the current path is equal to the
    /// <paramref name="obj" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj)
        => Equals(obj as FieldPath);

    /// <summary>
    /// Returns the hash code for this path.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
        => HashCode.Combine(Name, Parent);

    /// <summary>
    /// Returns a string that represents the current path.
    /// </summary>
    /// <returns>
    /// A string that represents the current path.
    /// </returns>
    public override string ToString()
    {
        var first = true;
        var path = new StringBuilder();
        var current = this;

        do
        {
            if (first)
            {
                path.Insert(0, current.Name);
                path.Insert(0, '.');
                first = false;
            }

            current = current.Parent;
        } while (current != null);

        return path.ToString();
    }

    /// <summary>
    /// Creates a new path segment from a string representation.
    /// </summary>
    /// <param name="s">
    /// The string representation of the path.
    /// </param>
    /// <returns>
    /// Returns a new path segment.
    /// </returns>
    public static FieldPath Parse(string s)
    {
        var path = Root;

        foreach (var element in s.Split("."))
        {
            path = path.Append(element);
        }

        return path;
    }
}

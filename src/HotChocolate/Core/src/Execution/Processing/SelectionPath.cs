using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents GraphQL selection path which is used in the operation compiler.
/// </summary>
public sealed class SelectionPath : IEquatable<SelectionPath>
{
    private SelectionPath(string name, SelectionPath? parent = null)
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
    public SelectionPath? Parent { get; }

    /// <summary>
    /// Gets the root path segment.
    /// </summary>
    public static SelectionPath Root { get; } = new("$root");

    /// <summary>
    /// Creates a new path segment.
    /// </summary>
    /// <param name="name">
    /// The name of the path segment.
    /// </param>
    /// <returns>
    /// Returns a new path segment.
    /// </returns>
    public SelectionPath Append(string name) => new(name, this);

    /// <summary>
    /// Indicates whether the current path is equal to another path.
    /// </summary>
    /// <param name="other">A path to compare with this path.</param>
    /// <returns>
    /// <see langword="true" /> if the current path is equal to the
    /// <paramref name="other" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(SelectionPath? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Name.EqualsOrdinal(other.Name))
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
        => Equals(obj as SelectionPath);

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
        var path = new StringBuilder();
        var current = this;

        do
        {
            path.Insert(0, current.Name);
            path.Insert(0, '/');
            current = current.Parent;
        } while (current != null);

        return path.ToString();
    }
}

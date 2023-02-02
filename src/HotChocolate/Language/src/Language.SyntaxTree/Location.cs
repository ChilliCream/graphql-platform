using System;

namespace HotChocolate.Language;

/// <summary>
/// The location of a <see cref="ISyntaxNode"/>.
/// </summary>
public sealed class Location : IEquatable<Location>
{
    /// <summary>
    /// Initializes a new instance of <see cref="Location"/>.
    /// </summary>
    /// <param name="start">The start position of the <see cref="ISyntaxNode"/>.</param>
    /// <param name="end">The end position of the <see cref="ISyntaxNode"/>.</param>
    /// <param name="line">The line in which the <see cref="ISyntaxNode"/> is located.</param>
    /// <param name="column">The column in which the <see cref="ISyntaxNode"/> is located.</param>
    public Location(int start, int end, int line, int column)
    {
        Start = start;
        End = end;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the character offset at which this
    /// <see cref="ISyntaxNode" /> begins.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Gets the character offset at which this
    /// <see cref="ISyntaxNode" /> ends.
    /// </summary>
    public int End { get; }

    /// <summary>
    /// Gets the 1-indexed line number on which this
    /// <see cref="ISyntaxNode" /> appears.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the 1-indexed column number at which this
    /// <see cref="ISyntaxNode" /> begins.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">
    /// An object to compare with this object.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the current object is equal to the
    /// <paramref name="other" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(Location? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Start == other.Start &&
            End == other.End &&
            Line == other.Line &&
            Column == other.Column;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with the current object.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified object  is equal to the current object;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) ||
            obj is Location other &&
            Equals(other);

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Start;
            hashCode = (hashCode * 397) ^ End;
            hashCode = (hashCode * 397) ^ Line;
            hashCode = (hashCode * 397) ^ Column;
            return hashCode;
        }
    }

    public static bool operator ==(Location? left, Location? right)
        => Equals(left, right);

    public static bool operator !=(Location? left, Location? right)
        => !Equals(left, right);
}

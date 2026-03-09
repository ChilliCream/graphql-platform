namespace HotChocolate;

/// <summary>
/// Represents a location in a GraphQL source file.
/// </summary>
public readonly struct Location : IComparable<Location>, IEquatable<Location>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Location"/> struct.
    /// </summary>
    /// <param name="line">
    /// The line number of the location. The first line is 1.
    /// </param>
    /// <param name="column">
    /// The column number of the location. The first column is 1.
    /// </param>
    public Location(int line, int column)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(line, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(column, 1);
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the line number of the location. The first line is 1.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the column number of the location. The first column is 1.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Compares this instance with another <see cref="Location"/>
    /// </summary>
    /// <param name="other">
    /// The <see cref="Location"/> to compare with this instance.
    /// </param>
    /// <returns>
    /// A signed number indicating the relative values of this instance
    /// </returns>
    public int CompareTo(Location other)
    {
        var lineComparison = Line.CompareTo(other.Line);

        if (lineComparison != 0)
        {
            return lineComparison;
        }

        return Column.CompareTo(other.Column);
    }

    /// <summary>
    /// Determines whether the specified <paramref name="other"/> location is equal to this location instance.
    /// </summary>
    /// <param name="other">
    /// The <see cref="Location"/> to compare with this instance.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="other"/> location is equal to this location instance;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(Location other)
        => Line == other.Line && Column == other.Column;

    /// <summary>
    /// Determines whether the specified <paramref name="obj"/> is equal to this location instance.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with this instance.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="obj"/> is equal to this location instance;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
        => obj is Location other && Equals(other);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms
    /// and data structures such as a hash table.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(Line, Column);

    /// <summary>
    /// Compares two <see cref="Location"/> instances for equality.
    /// </summary>
    /// <param name="left">
    /// The left <see cref="Location"/> to compare.
    /// </param>
    /// <param name="right">
    /// The right <see cref="Location"/> to compare.
    /// </param>
    /// <returns>
    /// <c>true</c> if the two <see cref="Location"/> instances are equal;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool operator ==(Location left, Location right)
        => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="Location"/> instances for inequality.
    /// </summary>
    /// <param name="left">
    /// The left <see cref="Location"/> to compare.
    /// </param>
    /// <param name="right">
    /// The right <see cref="Location"/> to compare.
    /// </param>
    /// <returns>
    /// <c>true</c> if the two <see cref="Location"/> instances are not equal;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool operator !=(Location left, Location right)
        => !left.Equals(right);
}

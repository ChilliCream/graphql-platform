using System.Collections;

namespace HotChocolate;

/// <summary>
/// Represents an ordered path of <see cref="SchemaCoordinate"/> values
/// from a schema element to a root type.
/// </summary>
public sealed class SchemaCoordinatePath : IReadOnlyList<SchemaCoordinate>
{
    private readonly SchemaCoordinate[] _segments;

    /// <summary>
    /// Initializes a new instance of <see cref="SchemaCoordinatePath"/>.
    /// </summary>
    /// <param name="segments">
    /// The ordered sequence of schema coordinates that form the path.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="segments"/> is empty.
    /// </exception>
    public SchemaCoordinatePath(ReadOnlySpan<SchemaCoordinate> segments)
    {
        if (segments.Length == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(segments),
                segments.Length,
                "A schema coordinate path must contain at least one segment.");
        }

        _segments = segments.ToArray();
    }

    /// <inheritdoc />
    public int Count => _segments.Length;

    /// <inheritdoc />
    public SchemaCoordinate this[int index] => _segments[index];

    /// <inheritdoc />
    public IEnumerator<SchemaCoordinate> GetEnumerator()
    {
        foreach (var segment in _segments)
        {
            yield return segment;
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

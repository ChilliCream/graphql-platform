using System.Collections;
using System.Collections.Immutable;

namespace HotChocolate;

/// <summary>
/// Represents an ordered path of <see cref="SchemaCoordinate"/> values
/// from a schema element to a root type.
/// </summary>
public sealed class SchemaCoordinatePath : IReadOnlyList<SchemaCoordinate>
{
    private readonly SchemaCoordinate[] _segments;
    private ImmutableArray<string>? _stringSegments;

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

    /// <summary>
    /// Returns the path segments as an immutable array of their string representations.
    /// The result is cached for subsequent calls.
    /// </summary>
    /// <returns>
    /// An <see cref="ImmutableArray{T}"/> of schema coordinate strings,
    /// ordered from the matched element to the root type.
    /// </returns>
    public ImmutableArray<string> ToStringArray()
    {
        if (_stringSegments is not null)
        {
            return _stringSegments.Value;
        }

        lock (_segments)
        {
            if (_stringSegments is not null)
            {
                return _stringSegments.Value;
            }

            var strings = _segments.Select(s => s.ToString()).ToImmutableArray();
            _stringSegments = strings;
            return strings;
        }
    }

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

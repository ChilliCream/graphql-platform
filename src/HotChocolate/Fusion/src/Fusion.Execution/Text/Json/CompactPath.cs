using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Represents a path through a GraphQL result tree using integer segments.
/// The sign bit distinguishes between the two segment kinds:
/// positive values are field selection IDs, and negative values are array indices
/// (stored as the bitwise complement of the index).
/// </summary>
public readonly struct CompactPath : IEquatable<CompactPath>
{
    /// <summary>
    /// Gets the empty root path.
    /// </summary>
    public static CompactPath Root => default;

    private readonly int[]? _segments;

    internal CompactPath(int[] segments)
        => _segments = segments;

    /// <summary>
    /// Gets the path segments as a read-only span.
    /// </summary>
    public ReadOnlySpan<int> Segments
        => _segments is null
            ? ReadOnlySpan<int>.Empty
            : _segments.AsSpan(1, _segments[0]);

    /// <summary>
    /// Gets the number of segments in the path.
    /// </summary>
    public int Length => _segments?[0] ?? 0;

    /// <summary>
    /// Gets a value indicating whether this is the root path (i.e. has no segments).
    /// </summary>
    public bool IsRoot => _segments is null;

    /// <summary>
    /// Gets the segment at the specified index.
    /// </summary>
    /// <param name="index">The zero-based segment index.</param>
    public int this[int index] => _segments![index + 1];

    internal int[]? UnsafeGetBackingArray() => _segments;

    /// <summary>
    /// Converts this compact path into a <see cref="Path"/> by resolving
    /// selection IDs to their response names using the given operation.
    /// </summary>
    /// <param name="operation">The operation used to resolve selection IDs.</param>
    public Path ToPath(Operation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var path = Path.Root;

        if (_segments is null)
        {
            return path;
        }

        var length = _segments[0];
        for (var i = 1; i <= length; i++)
        {
            var segment = _segments[i];

            if (segment < 0)
            {
                path = path.Append(~segment);
            }
            else
            {
                path = path.Append(operation.GetSelectionById(segment).ResponseName);
            }
        }

        return path;
    }

    /// <summary>
    /// Converts this compact path into a <see cref="Path"/> and appends an array index segment.
    /// </summary>
    /// <param name="operation">The operation used to resolve selection IDs.</param>
    /// <param name="appendIndex">The array index to append.</param>
    public Path ToPath(Operation operation, int appendIndex)
        => ToPath(operation).Append(appendIndex);

    /// <summary>
    /// Converts this compact path into a <see cref="Path"/> and appends a field name segment.
    /// </summary>
    /// <param name="operation">The operation used to resolve selection IDs.</param>
    /// <param name="appendField">The field name to append.</param>
    public Path ToPath(Operation operation, string appendField)
        => ToPath(operation).Append(appendField);

    /// <inheritdoc />
    public bool Equals(CompactPath other)
        => Segments.SequenceEqual(other.Segments);

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is CompactPath other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        if (_segments is not null)
        {
            var length = _segments[0];
            for (var i = 1; i <= length; i++)
            {
                hashCode.Add(_segments[i]);
            }
        }

        return hashCode.ToHashCode();
    }

    public static bool operator ==(CompactPath left, CompactPath right)
        => left.Equals(right);

    public static bool operator !=(CompactPath left, CompactPath right)
        => !left.Equals(right);
}

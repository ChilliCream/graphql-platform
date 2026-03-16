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
    public static CompactPath Root => default;

    private readonly int[]? _segments;

    internal CompactPath(int[] segments)
        => _segments = segments;

    public ReadOnlySpan<int> Segments
        => _segments is null
            ? ReadOnlySpan<int>.Empty
            : _segments.AsSpan(1, _segments[0]);

    public int Length => _segments?[0] ?? 0;

    public bool IsRoot => _segments is null;

    public int this[int index] => _segments![index + 1];

    internal int[]? UnsafeGetBackingArray() => _segments;

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

    public Path ToPath(Operation operation, int appendIndex)
        => ToPath(operation).Append(appendIndex);

    public Path ToPath(Operation operation, string appendField)
        => ToPath(operation).Append(appendField);

    public bool Equals(CompactPath other)
        => Segments.SequenceEqual(other.Segments);

    public override bool Equals(object? obj)
        => obj is CompactPath other && Equals(other);

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

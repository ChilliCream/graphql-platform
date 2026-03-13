using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// A compact, integer-based path representation for the Fusion execution engine.
/// Each segment is either a positive Selection ID (field) or a bitwise-NOT array index (negative).
/// </summary>
public readonly struct CompactPath : IEquatable<CompactPath>
{
    public static CompactPath Root => default;

    private readonly int[]? _segments;

    internal CompactPath(int[] segments)
        => _segments = segments;

    public ReadOnlySpan<int> Segments
        => _segments ?? ReadOnlySpan<int>.Empty;

    public int Length => _segments?.Length ?? 0;

    public bool IsRoot => _segments is null;

    public int this[int index] => _segments![index];

    public Path ToPath(Operation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var path = Path.Root;

        if (_segments is null)
        {
            return path;
        }

        for (var i = 0; i < _segments.Length; i++)
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
            for (var i = 0; i < _segments.Length; i++)
            {
                hashCode.Add(_segments[i]);
            }
        }

        return hashCode.ToHashCode();
    }
}

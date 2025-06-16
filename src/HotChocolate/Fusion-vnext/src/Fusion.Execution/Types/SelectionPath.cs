using System.Collections.Immutable;
using System.Text;

namespace HotChocolate.Fusion.Types;

public sealed class SelectionPath : IEquatable<SelectionPath>
{
    private readonly ImmutableArray<Segment> _segments;

    private SelectionPath(ImmutableArray<Segment> segments)
    {
        _segments = segments;
    }

    public bool IsRoot => _segments.IsEmpty;

    public SelectionPath? Parent =>
        _segments.IsEmpty ? null : new SelectionPath(_segments.RemoveAt(_segments.Length - 1));

    public ImmutableArray<Segment> Segments => _segments;

    public SelectionPath AppendField(string fieldName) =>
        new(_segments.Add(new Segment(fieldName, SelectionPathSegmentKind.Field)));

    public SelectionPath AppendFragment(string typeName) =>
        new(_segments.Add(new Segment(typeName, SelectionPathSegmentKind.InlineFragment)));

    public static SelectionPath Root { get; } = new([]);

    public static SelectionPath Parse(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return Root;
        }

        var builder = ImmutableArray.CreateBuilder<Segment>();

        foreach (var segment in s.Split('.'))
        {
            builder.Add(
                segment[0] == '<'
                    ? new Segment(segment[1..^1], SelectionPathSegmentKind.InlineFragment)
                    : new Segment(segment, SelectionPathSegmentKind.Field));
        }

        return new SelectionPath(builder.ToImmutable());
    }

    public override string ToString()
    {
        if (_segments.IsEmpty)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        foreach (var seg in _segments)
        {
            if (sb.Length > 0)
            {
                sb.Append('.');
            }

            if (seg.Kind == SelectionPathSegmentKind.InlineFragment)
            {
                sb.Append('<').Append(seg.Name).Append('>');
            }
            else
            {
                sb.Append(seg.Name);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns the portion of this path that comes <em>after</em> <paramref name="basePath"/>.
    /// </summary>
    /// <param name="basePath">
    /// The path to remove from the start of this instance.
    /// Must be the same as, or a parent of, the current path.
    /// </param>
    /// <returns>
    /// A new <see cref="SelectionPath"/> representing the relative path,
    /// or <see cref="SelectionPath.Root"/> if both paths are identical.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="basePath"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="basePath"/> is not a parent of, or identical to, this path.
    /// </exception>
    public SelectionPath RelativeTo(SelectionPath basePath)
    {
        if (Equals(basePath))
        {
            return Root;
        }

        if (!basePath.IsParentOfOrSame(this))
        {
            throw new ArgumentException(nameof(basePath));
        }

        return new SelectionPath(_segments.RemoveRange(0, basePath._segments.Length));
    }

    public bool IsParentOfOrSame(SelectionPath other)
    {
        if (other._segments.Length < _segments.Length)
        {
            return false;
        }

        for (var i = 0; i < _segments.Length; i++)
        {
            if (_segments[i].Kind != other._segments[i].Kind ||
                !string.Equals(_segments[i].Name, other._segments[i].Name, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public bool Equals(SelectionPath? other) =>
        other is not null && _segments.SequenceEqual(other._segments);

    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || obj is SelectionPath p && Equals(p);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var s in _segments)
        {
            hash.Add(s.Name);
            hash.Add((int)s.Kind);
        }
        return hash.ToHashCode();
    }

    public static bool operator ==(SelectionPath? a, SelectionPath? b) => Equals(a, b);
    public static bool operator !=(SelectionPath? a, SelectionPath? b) => !Equals(a, b);

    public sealed record Segment(string Name, SelectionPathSegmentKind Kind);
}

using System.Collections.Immutable;
using System.Text;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class SelectionPath : IEquatable<SelectionPath>
{
    private readonly ImmutableArray<Segment> _segments;

    private SelectionPath(ImmutableArray<Segment> segments)
    {
        _segments = segments;
    }

    public bool IsRoot => _segments.IsEmpty;

    public SelectionPath? Parent
        => _segments.Length > 0
            ? new SelectionPath(_segments.RemoveAt(_segments.Length - 1))
            : null;

    public ImmutableArray<Segment> Segments => _segments;

    public SelectionPath AppendField(string fieldName)
        => new(_segments.Add(new Segment(fieldName, SelectionPathSegmentKind.Field)));

    public SelectionPath AppendFragment(string typeName)
        => new(_segments.Add(new Segment(typeName, SelectionPathSegmentKind.InlineFragment)));

    public static SelectionPath Root { get; } = new([]);

    public static SelectionPath Parse(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return Root;
        }

        // Remove the leading '$' if present
        if (s.StartsWith('$'))
        {
            s = s[1..];
        }

        if (string.IsNullOrEmpty(s))
        {
            return Root;
        }

        var builder = ImmutableArray.CreateBuilder<Segment>();
        var i = 0;

        while (i < s.Length)
        {
            switch (s[i])
            {
                case '.':
                    // Skip the dot and parse the field name
                    i++;
                    var fieldStart = i;

                    // Find the end of the field name (either end of string, '<', or '.')
                    while (i < s.Length && s[i] != '<' && s[i] != '.')
                    {
                        i++;
                    }

                    if (i > fieldStart)
                    {
                        var fieldName = s[fieldStart..i];
                        builder.Add(new Segment(fieldName, SelectionPathSegmentKind.Field));
                    }

                    break;

                case '<':
                    // Parse inline fragment
                    i++; // Skip '<'
                    var fragmentStart = i;

                    // Find the closing '>'
                    while (i < s.Length && s[i] != '>')
                    {
                        i++;
                    }

                    if (i < s.Length && s[i] == '>')
                    {
                        var fragmentName = s[fragmentStart..i];
                        builder.Add(new Segment(fragmentName, SelectionPathSegmentKind.InlineFragment));
                        i++; // Skip '>'
                    }
                    else
                    {
                        throw new ArgumentException("Invalid path format: unclosed inline fragment", nameof(s));
                    }

                    break;

                default:
                    // This should not happen with valid input
                    throw new ArgumentException($"Invalid character '{s[i]}' in path", nameof(s));
            }
        }

        return new SelectionPath(builder.ToImmutable());
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
    /// or <see cref="Root"/> if both paths are identical.
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

        return !basePath.IsParentOfOrSame(this)
            ? throw new ArgumentException(null, nameof(basePath))
            : new SelectionPath(_segments.RemoveRange(0, basePath._segments.Length));
    }

    public bool IsParentOfOrSame(SelectionPath other)
    {
        if (other._segments.Length < _segments.Length)
        {
            return false;
        }

        for (var i = 0; i < _segments.Length; i++)
        {
            if (_segments[i].Kind != other._segments[i].Kind
                || !string.Equals(_segments[i].Name, other._segments[i].Name, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public bool Equals(SelectionPath? other)
        => other is not null && _segments.SequenceEqual(other._segments);

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || obj is SelectionPath p && Equals(p);

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

    public override string ToString()
    {
        if (_segments.IsEmpty)
        {
            return "$";
        }

        var sb = new StringBuilder();
        sb.Append('$');

        // Iterate forward through segments, not backward
        for (var i = 0; i < _segments.Length; i++)
        {
            var seg = _segments[i];

            if (seg.Kind == SelectionPathSegmentKind.InlineFragment)
            {
                sb.Append('<').Append(seg.Name).Append('>');
            }
            else
            {
                sb.Append('.').Append(seg.Name);
            }
        }

        return sb.ToString();
    }

    public static bool operator ==(SelectionPath? a, SelectionPath? b) => Equals(a, b);

    public static bool operator !=(SelectionPath? a, SelectionPath? b) => !Equals(a, b);

    public sealed record Segment(string Name, SelectionPathSegmentKind Kind);
}

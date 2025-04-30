using System.Collections.Immutable;
using System.Text;

namespace HotChocolate.Fusion.Types;

public sealed class SelectionPath : IEquatable<SelectionPath>
{
    private readonly ImmutableArray<SelectionPath> _segments;

    private SelectionPath(
        SelectionPath? parent,
        string name,
        SelectionPathSegmentKind kind)
    {
        _segments = parent is not null
            ? parent._segments.Add(this)
            : ImmutableArray<SelectionPath>.Empty.Add(this);
        Name = name;
        Kind = kind;
    }

    public SelectionPath? Parent
        => _segments.Length > 0 ? _segments[^1] : null;

    public string Name { get; }

    public bool IsRoot => Kind == SelectionPathSegmentKind.Root;

    public SelectionPathSegmentKind Kind { get; }

    public ImmutableArray<SelectionPath> Segments => _segments;

    public SelectionPath AppendField(string fieldName)
    {
        return Kind == SelectionPathSegmentKind.Root
            ? new SelectionPath(null, fieldName, SelectionPathSegmentKind.Field)
            : new SelectionPath(this, fieldName, SelectionPathSegmentKind.Field);
    }

    public SelectionPath AppendFragment(string typeName)
    {
        return Kind == SelectionPathSegmentKind.Root
            ? new SelectionPath(null, typeName, SelectionPathSegmentKind.InlineFragment)
            : new SelectionPath(this, typeName, SelectionPathSegmentKind.InlineFragment);
    }

    public static SelectionPath Root { get; } =
        new(null, "root", SelectionPathSegmentKind.Root);

    public static SelectionPath Parse(string s)
    {
        var current = Root;

        foreach (var segment in s.Split("."))
        {
            if (segment.StartsWith('<'))
            {
                var typeName = segment[1..^1];
                current = current.AppendFragment(typeName);
            }
            else
            {
                current = current.AppendField(segment);
            }
        }

        return current;
    }

    public override string ToString()
    {
        var path = new StringBuilder();

        foreach (var segment in _segments)
        {
            if (segment.Kind == SelectionPathSegmentKind.Root)
            {
                continue;
            }

            if (path.Length > 0)
            {
                path.Append('.');
            }

            if (segment.Kind == SelectionPathSegmentKind.InlineFragment)
            {
                path.Append('<');
                path.Append(segment.Name);
                path.Append('>');
            }
            else
            {
                path.Append(segment.Name);
            }
        }

        return path.ToString();
    }

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

        return Kind == other.Kind
            && Name == other.Name
            && _segments.SequenceEqual(other._segments);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj)
            || (obj is SelectionPath other && Equals(other));

    public bool IsParentOfOrSame(SelectionPath path)
    {
        if(Equals(path))
        {
            return true;
        }

        if (Segments.Length >= path.Segments.Length - 1)
        {
            return false;
        }

        for (var i = 0; i < path.Segments.Length; i++)
        {
            if (!Segments[i].Name.Equals(path.Segments[i].Name , StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
        => HashCode.Combine(_segments, Name, (int)Kind);

    public static bool operator ==(SelectionPath? left, SelectionPath? right)
        => Equals(left, right);

    public static bool operator !=(SelectionPath? left, SelectionPath? right)
        => !Equals(left, right);
}

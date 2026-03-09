using System.Collections.Immutable;
using System.Text;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a path to a selection set or a field selection within a GraphQL operation.
/// </summary>
public sealed class SelectionPath : IEquatable<SelectionPath>
{
    private readonly Segment[] _segments;
    private readonly int _length;

    private SelectionPath(Segment[] segments, int length)
    {
        _segments = segments;
        _length = length;
    }

    /// <summary>
    /// Gets a value indicating whether this path represents the root of an operation.
    /// The root of an operation is the root selection set.
    /// </summary>
    public bool IsRoot => _length == 0;

    /// <summary>
    /// Gets the name of the last segment in the path, or <c>null</c> if the path is root.
    /// </summary>
    public string? Name => _length > 0 ? _segments[_length - 1].Name : null;

    /// <summary>
    /// Gets the parent path.
    /// </summary>
    public SelectionPath? Parent
        => _length > 0
            ? new SelectionPath(_segments, _length - 1)
            : null;

    /// <summary>
    /// Gets the number of segments in this path.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Gets the segment at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the segment to get.</param>
    /// <returns>The segment at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is less than 0 or greater than or equal to <see cref="Length"/>.
    /// </exception>
    public Segment this[int index] => _segments[index];

    /// <summary>
    /// Returns a new <see cref="SelectionPath"/> sharing the same backing array
    /// but with the specified number of segments.
    /// </summary>
    /// <param name="length">The number of segments to include.</param>
    /// <returns>A new <see cref="SelectionPath"/> with the specified length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="length"/> is less than 0 or greater than <see cref="Length"/>.
    /// </exception>
    public SelectionPath Slice(int length)
    {
        if ((uint)length > (uint)_length)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (length == 0)
        {
            return Root;
        }

        return new SelectionPath(_segments, length);
    }

    /// <summary>
    /// Creates a new selection path by appending a field segment to the current path.
    /// </summary>
    /// <param name="fieldName">The name of the field to append.</param>
    /// <returns>A new <see cref="SelectionPath"/> with the field appended.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="fieldName"/> is <c>null</c>.
    /// </exception>
    public SelectionPath AppendField(string fieldName)
    {
        var newSegments = new Segment[_length + 1];
        Array.Copy(_segments, newSegments, _length);
        newSegments[_length] = new Segment(fieldName, SelectionPathSegmentKind.Field);
        return new SelectionPath(newSegments, newSegments.Length);
    }

    /// <summary>
    /// Creates a new selection path by appending an inline fragment segment to the current path.
    /// </summary>
    /// <param name="typeName">The name of the type condition for the inline fragment.</param>
    /// <returns>A new <see cref="SelectionPath"/> with the inline fragment appended.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="typeName"/> is <c>null</c>.
    /// </exception>
    public SelectionPath AppendFragment(string typeName)
    {
        var newSegments = new Segment[_length + 1];
        Array.Copy(_segments, newSegments, _length);
        newSegments[_length] = new Segment(typeName, SelectionPathSegmentKind.InlineFragment);
        return new SelectionPath(newSegments, newSegments.Length);
    }

    /// <summary>
    /// Gets the root selection path (empty path).
    /// </summary>
    public static SelectionPath Root { get; } = new([], 0);

    /// <summary>
    /// Parses a string representation of a selection path into a <see cref="SelectionPath"/> instance.
    /// </summary>
    /// <param name="s">
    /// The string representation of the path. Should start with '$' and use '.' for field separators
    /// and '&lt;TypeName&gt;' for inline fragments.
    /// </param>
    /// <returns>
    /// A <see cref="SelectionPath"/> representing the parsed path, or <see cref="Root"/> if the
    /// input is null, empty, or represents the root path.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the input string contains invalid syntax, such as unclosed inline fragments
    /// or invalid characters.
    /// </exception>
    /// <example>
    /// <code>
    /// var path1 = SelectionPath.Parse("$.user.profile");      // Field path
    /// var path2 = SelectionPath.Parse("$&lt;User&gt;.name");  // Fragment + field
    /// var path3 = SelectionPath.Parse("$");                   // Root path
    /// </code>
    /// </example>
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

        var builder = new List<Segment>();
        var i = 0;

        while (i < s.Length)
        {
            switch (s[i])
            {
                // parse a field selection
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

                // Parse inline fragment
                case '<':
                    // Skip '<'
                    i++;
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

                        // Skip '>'
                        i++;
                    }
                    else
                    {
                        throw new ArgumentException("Invalid path format: unclosed inline fragment", nameof(s));
                    }

                    break;

                default:
                    throw new ArgumentException($"Invalid character '{s[i]}' in path", nameof(s));
            }
        }

        var segments = builder.ToArray();
        return new SelectionPath(segments, segments.Length);
    }

    /// <summary>
    /// Constructs <see cref="SelectionPath"/> from the given <paramref name="segments"/>.
    /// </summary>
    /// <param name="segments">
    /// The segments to initialize the <see cref="SelectionPath"/> with.
    /// </param>
    /// <returns>
    /// A new <see cref="SelectionPath"/> initialized with the <paramref name="segments"/>.
    /// </returns>
    public static SelectionPath From(ImmutableArray<Segment> segments)
    {
        var array = segments.AsSpan().ToArray();
        return new SelectionPath(array, array.Length);
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
    /// <example>
    /// <code>
    /// var fullPath = SelectionPath.Parse("$.user.profile.name");
    /// var basePath = SelectionPath.Parse("$.user");
    /// var relativePath = fullPath.RelativeTo(basePath); // Results in "$.profile.name"
    /// </code>
    /// </example>
    public SelectionPath RelativeTo(SelectionPath basePath)
    {
        if (Equals(basePath))
        {
            return Root;
        }

        if (!basePath.IsParentOfOrSame(this))
        {
            throw new ArgumentException(null, nameof(basePath));
        }

        var newLength = _length - basePath._length;
        var newSegments = new Segment[newLength];
        Array.Copy(_segments, basePath._length, newSegments, 0, newLength);
        return new SelectionPath(newSegments, newLength);
    }

    /// <summary>
    /// Determines whether this path is a parent of, or the same as, the specified path.
    /// </summary>
    /// <param name="other">The path to compare against.</param>
    /// <returns>
    /// <c>true</c> if this path is a parent of or identical to <paramref name="other"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// var parentPath = SelectionPath.Parse("$.user");
    /// var childPath = SelectionPath.Parse("$.user.profile");
    /// var isParent = parentPath.IsParentOfOrSame(childPath); // Returns true
    /// </code>
    /// </example>
    public bool IsParentOfOrSame(SelectionPath other)
    {
        if (other._length < _length)
        {
            return false;
        }

        for (var i = 0; i < _length; i++)
        {
            if (_segments[i].Kind != other._segments[i].Kind
                || !string.Equals(_segments[i].Name, other._segments[i].Name, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether this <see cref="SelectionPath"/> is equal to another <see cref="SelectionPath"/>.
    /// </summary>
    /// <param name="other">The <see cref="SelectionPath"/> to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="SelectionPath"/> is equal to this instance;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(SelectionPath? other)
    {
        if (other is null)
        {
            return false;
        }

        if (_length != other._length)
        {
            return false;
        }

        return _segments.AsSpan(0, _length).SequenceEqual(other._segments.AsSpan(0, other._length));
    }

    /// <summary>
    /// Determines whether this <see cref="SelectionPath"/> is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if the specified object is a <see cref="SelectionPath"/> and is equal to this instance;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || (obj is SelectionPath p && Equals(p));

    /// <summary>
    /// Returns a hash code for this <see cref="SelectionPath"/>.
    /// </summary>
    /// <returns>A hash code for the current <see cref="SelectionPath"/>.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        for (var i = 0; i < _length; i++)
        {
            hash.Add(_segments[i].Name);
            hash.Add((int)_segments[i].Kind);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Returns a string representation of this selection path.
    /// </summary>
    /// <returns>
    /// A string representation in the format '$' for root, '$.field' for fields,
    /// and '$&lt;Type&gt;.field' for inline fragments, with segments separated appropriately.
    /// </returns>
    /// <example>
    /// <code>
    /// SelectionPath.Root.ToString()                    // "$"
    /// path.AppendField("user").ToString()             // "$.user"
    /// path.AppendFragment("User").ToString()          // "$&lt;User&gt;"
    /// path.AppendField("user").AppendField("profile") // "$.user.profile"
    /// </code>
    /// </example>
    public override string ToString()
    {
        if (_length == 0)
        {
            return "$";
        }

        var sb = new StringBuilder();
        sb.Append('$');

        for (var i = 0; i < _length; i++)
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

    /// <summary>
    /// Determines whether two <see cref="SelectionPath"/> instances are equal.
    /// </summary>
    /// <param name="a">The first <see cref="SelectionPath"/> to compare.</param>
    /// <param name="b">The second <see cref="SelectionPath"/> to compare.</param>
    /// <returns>
    /// <c>true</c> if the <see cref="SelectionPath"/> instances are equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator ==(SelectionPath? a, SelectionPath? b) => Equals(a, b);

    /// <summary>
    /// Determines whether two <see cref="SelectionPath"/> instances are not equal.
    /// </summary>
    /// <param name="a">The first <see cref="SelectionPath"/> to compare.</param>
    /// <param name="b">The second <see cref="SelectionPath"/> to compare.</param>
    /// <returns>
    /// <c>true</c> if the <see cref="SelectionPath"/> instances are not equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator !=(SelectionPath? a, SelectionPath? b) => !Equals(a, b);

    /// <summary>
    /// Represents a single segment within a selection path.
    /// </summary>
    /// <param name="Name">The name of the field or type for this segment.</param>
    /// <param name="Kind">The kind of segment (field or inline fragment).</param>
    public sealed record Segment(string Name, SelectionPathSegmentKind Kind);

    /// <summary>
    /// Creates a new builder for creating <see cref="SelectionPath"/> instances.
    /// </summary>
    /// <returns>A new <see cref="Builder"/> instance.</returns>
    public static Builder CreateBuilder() => new();

    public static Builder CreateBuilder(SelectionPath path) => new(path);

    /// <summary>
    /// A builder for creating <see cref="SelectionPath"/> instances.
    /// </summary>
    public readonly struct Builder
    {
        private readonly List<Segment> _segments = [];

        /// <summary>
        /// Initializes new instance of <see cref="Builder"/>.
        /// </summary>
        public Builder()
        {
        }

        /// <summary>
        /// Initializes new instance of <see cref="Builder"/>.
        /// </summary>
        public Builder(SelectionPath path)
        {
            _segments.AddRange(path._segments.AsSpan(0, path._length));
        }

        /// <summary>
        /// Creates a new selection path by appending a field segment to the current path.
        /// </summary>
        /// <param name="fieldName">The name of the field to append.</param>
        /// <returns>A new <see cref="SelectionPath"/> with the field appended.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="fieldName"/> is <c>null</c>.
        /// </exception>
        public Builder AppendField(string fieldName)
        {
            _segments.Add(new Segment(fieldName, SelectionPathSegmentKind.Field));
            return this;
        }

        /// <summary>
        /// Creates a new selection path by appending an inline fragment segment to the current path.
        /// </summary>
        /// <param name="typeName">The name of the type condition for the inline fragment.</param>
        /// <returns>A new <see cref="SelectionPath"/> with the inline fragment appended.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="typeName"/> is <c>null</c>.
        /// </exception>
        public Builder AppendFragment(string typeName)
        {
            _segments.Add(new Segment(typeName, SelectionPathSegmentKind.InlineFragment));
            return this;
        }

        /// <summary>
        /// Builds a <see cref="SelectionPath"/> from the segments in the builder.
        /// </summary>
        /// <returns>A new <see cref="SelectionPath"/> with the segments in the builder.</returns>
        public SelectionPath Build()
        {
            var segments = _segments.ToArray();
            return new SelectionPath(segments, segments.Length);
        }
    }
}

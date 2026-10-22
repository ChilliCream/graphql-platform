namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// A lightweight read-only view over a contiguous region of <see cref="CompactPath"/> values.
/// Multiple segments can share the same backing array, avoiding per-slot array allocations
/// when distributing additional paths across variable value sets.
/// </summary>
public readonly struct CompactPathSegment
{
    private readonly CompactPath[]? _array;
    private readonly CompactPathSource? _source;
    private readonly int _offset;
    private readonly int _count;

    internal CompactPathSegment(CompactPath[] array, int offset, int count)
    {
        _array = array;
        _offset = offset;
        _count = count;
    }

    internal CompactPathSegment(CompactPathSource source, int offset, int count)
    {
        _source = source;
        _offset = offset;
        _count = count;
    }

    /// <summary>
    /// Gets the source that creates the paths of this segment on first access,
    /// or <c>null</c> if the paths already exist.
    /// </summary>
    internal CompactPathSource? Source => _source;

    /// <summary>
    /// Gets the offset of this segment within its backing array or source.
    /// </summary>
    internal int Offset => _offset;

    /// <summary>
    /// Gets the number of paths in this segment.
    /// </summary>
    public int Length => _count;

    /// <summary>
    /// Gets a value indicating whether this segment is empty.
    /// </summary>
    public bool IsDefaultOrEmpty => _count == 0;

    /// <summary>
    /// Gets the paths as a read-only span.
    /// </summary>
    public ReadOnlySpan<CompactPath> AsSpan()
        => GetArray() is { } array
            ? array.AsSpan(_offset, _count)
            : ReadOnlySpan<CompactPath>.Empty;

    /// <summary>
    /// Gets the path at the specified index.
    /// </summary>
    public CompactPath this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return GetArray()![_offset + index];
        }
    }

    /// <summary>
    /// Returns a copy of this segment whose paths are backed by freshly allocated
    /// arrays, so they stay valid after the original backing buffers are reused.
    /// </summary>
    internal CompactPathSegment Detach()
    {
        if (_count == 0)
        {
            return default;
        }

        var array = GetArray()!;
        var paths = new CompactPath[_count];

        for (var i = 0; i < _count; i++)
        {
            paths[i] = array[_offset + i].Detach();
        }

        return new CompactPathSegment(paths, 0, _count);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the paths.
    /// </summary>
    public Enumerator GetEnumerator() => new(GetArray(), _offset, _count);

    private CompactPath[]? GetArray()
        => _array ?? (_count == 0 ? null : _source?.GetPaths());

    /// <summary>
    /// Enumerates the paths in a <see cref="CompactPathSegment"/>.
    /// </summary>
    public struct Enumerator
    {
        private readonly CompactPath[]? _array;
        private readonly int _end;
        private int _index;

        internal Enumerator(CompactPath[]? array, int offset, int count)
        {
            _array = array;
            _end = offset + count;
            _index = offset - 1;
        }

        /// <summary>
        /// Gets the current path.
        /// </summary>
        public CompactPath Current => _array![_index];

        /// <summary>
        /// Advances to the next path.
        /// </summary>
        public bool MoveNext() => ++_index < _end;
    }
}

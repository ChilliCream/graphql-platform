namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// A lightweight read-only view over a contiguous region of <see cref="CompactPath"/> values.
/// Multiple segments can share the same backing array, avoiding per-slot array allocations
/// when distributing additional paths across variable value sets.
/// </summary>
public readonly struct CompactPathSegment
{
    private readonly CompactPath[]? _array;
    private readonly int _offset;
    private readonly int _count;

    internal CompactPathSegment(CompactPath[] array, int offset, int count)
    {
        _array = array;
        _offset = offset;
        _count = count;
    }

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
        => _array is null
            ? ReadOnlySpan<CompactPath>.Empty
            : _array.AsSpan(_offset, _count);

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

            return _array![_offset + index];
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the paths.
    /// </summary>
    public Enumerator GetEnumerator() => new(_array, _offset, _count);

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

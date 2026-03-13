using System.Buffers;
using System.Diagnostics;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Stack-based builder for <see cref="CompactPath"/>. Starts on a caller-supplied
/// stack buffer and spills to <see cref="ArrayPool{T}"/> if the path exceeds it.
/// </summary>
internal ref struct CompactPathBuilder
{
    private readonly PathSegmentLocalPool? _pool;
    private Span<int> _span;
    private int[]? _arrayFromPool;
    private int _pos;

    public CompactPathBuilder(Span<int> initialBuffer, PathSegmentLocalPool? pool)
    {
        Debug.Assert(initialBuffer.Length > 0);

        _span = initialBuffer;
        _pool = pool;
        _arrayFromPool = null;
        _pos = 0;
    }

    public void Append(int segment)
    {
        if (_pos == _span.Length)
        {
            Grow();
        }

        _span[_pos++] = segment;
    }

    public void AppendField(int selectionId) => Append(selectionId);

    public void AppendIndex(int arrayIndex) => Append(~arrayIndex);

    public CompactPath ToPath()
    {
        if (_pos == 0)
        {
            ReturnPooledArray();
            return CompactPath.Root;
        }

        if (_pool is null)
        {
            return ToPathNoPool();
        }

        // -1 because [0] is reserved for the length
        if (_pos <= PathSegmentMemory.SegmentArraySize - 1)
        {
            var array = _pool.Rent();
            array[0] = _pos;
            _span[.._pos].CopyTo(array.AsSpan(1));
            ReturnPooledArray();
            return new CompactPath(array);
        }

        // Overflow: path deeper than 31 — allocate exact-sized array (extremely rare)
        var overflow = new int[_pos + 1];
        overflow[0] = _pos;
        _span[.._pos].CopyTo(overflow.AsSpan(1));
        ReturnPooledArray();
        return new CompactPath(overflow);
    }

    private CompactPath ToPathNoPool()
    {
        if (_pos == 0)
        {
            ReturnPooledArray();
            return CompactPath.Root;
        }

        var result = new int[_pos + 1];
        result[0] = _pos;
        _span[.._pos].CopyTo(result.AsSpan(1));
        ReturnPooledArray();
        return new CompactPath(result);
    }

    private void ReturnPooledArray()
    {
        if (_arrayFromPool is not null)
        {
            ArrayPool<int>.Shared.Return(_arrayFromPool);
            _arrayFromPool = null;
        }
    }

    private void Grow()
    {
        var newArray = ArrayPool<int>.Shared.Rent(_span.Length * 2);
        _span[.._pos].CopyTo(newArray);

        if (_arrayFromPool is not null)
        {
            ArrayPool<int>.Shared.Return(_arrayFromPool);
        }

        _arrayFromPool = newArray;
        _span = newArray;
    }
}

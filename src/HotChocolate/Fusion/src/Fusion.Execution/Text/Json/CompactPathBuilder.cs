using System.Buffers;

namespace HotChocolate.Fusion.Text.Json;

internal ref struct CompactPathBuilder
{
    private Span<int> _span;
    private int[]? _arrayFromPool;
    private int _pos;

    public CompactPathBuilder(Span<int> initialBuffer)
    {
        if (initialBuffer.Length == 0)
        {
            throw new ArgumentException("The initial buffer cannot be empty.", nameof(initialBuffer));
        }

        _span = initialBuffer;
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

    public void AppendField(int selectionId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(selectionId);

        Append(selectionId);
    }

    public void AppendIndex(int arrayIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        Append(~arrayIndex);
    }

    public CompactPath ToPath()
    {
        if (_pos == 0)
        {
            ReturnPooledArray();
            return CompactPath.Root;
        }

        var result = _span[.._pos].ToArray();
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

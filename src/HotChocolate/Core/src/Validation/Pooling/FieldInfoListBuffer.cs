using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Validation;

internal sealed class FieldInfoListBuffer
{
    private readonly List<FieldInfo>[] _buffer =
    [
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
    ];
    private readonly int _max = 16;
    private int _index = 0;

    public List<FieldInfo> Pop()
    {
        if (TryPop(out var list))
        {
            return list;
        }
        throw new InvalidOperationException("Buffer is used up.");
    }

    public bool TryPop([NotNullWhen(true)] out List<FieldInfo>? list)
    {
        if (_index < _max)
        {
            list = _buffer[_index++];
            return true;
        }

        list = null;
        return false;
    }

    public void Clear()
    {
        if (_index > 0)
        {
            for (var i = 0; i < _index; i++)
            {
                _buffer[i].Clear();
            }
        }
        _index = 0;
    }
}

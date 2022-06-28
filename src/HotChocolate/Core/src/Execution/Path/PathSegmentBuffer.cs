using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace HotChocolate.Execution;

internal abstract class PathSegmentBuffer<T> where T : class
{
    private readonly int _capacity;
    private readonly T?[] _buffer;
    protected int Index;

    public PathSegmentBuffer(int capacity)
    {
        _capacity = capacity;
        _buffer = new T[capacity];
    }

    public bool HasSpace() => Index < _capacity;

    public T Pop()
    {
        if (TryPop(out T? obj))
        {
            return obj;
        }

        throw new InvalidOperationException("Buffer is used up.");
    }

    public bool TryPop([NotNullWhen(true)] out T? obj)
    {
        var nextIndex = Interlocked.Increment(ref Index) - 1;
        if (nextIndex < _capacity)
        {
            if (_buffer[nextIndex] is not null)
            {
                obj = (T)_buffer[nextIndex];
                return true;
            }

            obj = Create(nextIndex);
            _buffer[nextIndex] = obj;
            return true;
        }

        obj = null;
        return false;
    }

    public void Reset()
    {
        if (Index == 0)
        {
            return;
        }

        if (Index >= _capacity)
        {
            Index = _capacity;
        }

        Clear(_buffer, Index);

        Index = 0;
    }

    protected abstract T Create(int index);

    protected abstract void Clear(T?[] buffer, int index);
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal sealed class PathSegmentBuffer<T> where T : class
{
    private readonly int _capacity;
    private readonly IPooledObjectPolicy<T> _policy;
    private readonly T?[] _buffer;
    private int _index;

    public PathSegmentBuffer(int capacity, IPooledObjectPolicy<T> policy)
    {
        _capacity = capacity;
        _policy = policy;
        _buffer = new T[capacity];
    }

    public bool HasSpace() => _index < _capacity;

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
        var nextIndex = Interlocked.Increment(ref _index) - 1;
        if (nextIndex < _capacity)
        {
            if (_buffer[nextIndex] is { } o)
            {
                obj = o;
                return true;
            }

            obj = _policy.Create();
            _buffer[nextIndex] = obj;
            return true;
        }

        obj = null;
        return false;
    }

    public void Reset()
    {
        if (_index == 0)
        {
            return;
        }

        if (_index >= _capacity)
        {
            _index = _capacity;
        }

        for (var i = 0; i < _index; i++)
        {
            if (!_policy.Return(_buffer[i]!))
            {
                _buffer[i] = null;
            }
        }

        _index = 0;
    }
}

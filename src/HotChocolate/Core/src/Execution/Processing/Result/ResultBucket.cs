using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing;

internal sealed class ResultBucket<T> where T : class
{
    private readonly int _capacity;
    private readonly IPooledObjectPolicy<T> _policy;
    private readonly T?[] _buffer;
    private int _index;

    public ResultBucket(int capacity, IPooledObjectPolicy<T> policy)
    {
        _capacity = capacity;
        _policy = policy;
        _buffer = new T[capacity];
    }

    public T Pop()
    {
        if (TryPop(out var obj))
        {
            return obj;
        }

        throw new InvalidOperationException("Buffer is used up.");
    }

    public bool TryPop([NotNullWhen(true)] out T? obj)
    {
        var nextIndex = Interlocked.Increment(ref _index);
        if (nextIndex < _capacity)
        {
            var buffered = _buffer[nextIndex];

            if(!ReferenceEquals(buffered, null))
            {
                obj = buffered;
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

        ref var mem = ref MemoryMarshal.GetReference(_buffer.AsSpan());
        ref var end = ref Unsafe.Add(ref mem, _index);

        while (Unsafe.IsAddressLessThan(ref mem, ref end))
        {
            if (mem is not null && !_policy.Return(mem))
            {
                mem = default;
            }

            mem = ref Unsafe.Add(ref mem, 1);
        }

        _index = 0;
    }
}

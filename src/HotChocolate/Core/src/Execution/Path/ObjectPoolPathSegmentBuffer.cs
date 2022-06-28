#if !NET6_0_OR_GREATER
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal sealed class ObjectPoolPathSegmentBuffer<T> : PathSegmentBuffer<T> where T : class
{
    private readonly IPooledObjectPolicy<T> _policy;
    private readonly T?[] _buffer;

    public ObjectPoolPathSegmentBuffer(int capacity, IPooledObjectPolicy<T> policy) : base(capacity)
    {
        _policy = policy;
        _buffer = new T[capacity];
    }

    protected override T Create(int index) => _policy.Create();

    protected override void Clear(T?[] buffer, int index)
    {
        for (var i = 0; i < Index; i++)
        {
            if (!_policy.Return(_buffer[i]!))
            {
                _buffer[i] = null;
            }
        }
    }
}
#endif

using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

internal sealed class ResultDataPoolSession<T> where T : ResultData, new()
{
    private readonly object _lock = new();
    private readonly List<ResultDataBatch<T>> _usedBatches = new(16);
    private readonly ObjectPool<ResultDataBatch<T>> _pool;
    private ResultDataBatch<T> _current;

    public ResultDataPoolSession(ObjectPool<ResultDataBatch<T>> pool)
    {
        _pool = pool;
        _current = pool.Get();
        _usedBatches.Add(_current);
    }

    public T Rent()
    {
        while (true)
        {
            if (_current.TryRent(out var item))
            {
                return item;
            }

            lock (_lock)
            {
                if (_current.TryRent(out item))
                {
                    return item;
                }

                // we get the next batch and try to rent a result data object again.
                var next = _pool.Get();
                if (next.TryRent(out item))
                {
                    _usedBatches.Add(next);
                    _current = next;
                    return item;
                }

                // if there is no pool corruption we should never hit
                // this as a new batch from the pool would always have
                // new poolable object available.
                _current = next;
                _usedBatches.Add(next);
            }
        }
    }

    public void Reset()
    {
        // it is expected that a single thread will own this instance during reset.
        foreach (var batch in _usedBatches)
        {
            _pool.Return(batch);
        }

        _usedBatches.Clear();
        _current = _pool.Get();

        if (_usedBatches.Capacity > 32)
        {
            _usedBatches.Capacity = 16;
        }
        _usedBatches.Add(_current);
    }
}

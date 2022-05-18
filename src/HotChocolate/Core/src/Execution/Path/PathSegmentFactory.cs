using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal class PathSegmentFactory<T> where T : class
{
    private readonly ObjectPool<PathSegmentBuffer<T>> _pool;
    private readonly object _lockObject = new();
    private PathSegmentBuffer<T>? _current;
    private List<PathSegmentBuffer<T>>? _buffers;

    public PathSegmentFactory(ObjectPool<PathSegmentBuffer<T>> pool)
    {
        _pool = pool;
    }

    private void Resize()
    {
        if (_current is null || !_current.HasSpace())
        {
            lock (_lockObject)
            {
                if (_current is null || !_current.HasSpace())
                {
                    if (_current is not null)
                    {
                        _buffers ??= new();
                        _buffers.Add(_current);
                    }

                    _current = _pool.Get();
                }
            }
        }
    }

    public T Get()
    {
        while (true)
        {
            if (_current is null || !_current.TryPop(out T? segment))
            {
                Resize();
                continue;
            }

            return segment;
        }
    }

    public void Clear()
    {
        if (_buffers is { Count: > 0 })
        {
            for (var i = 0; i < _buffers.Count; i++)
            {
                _pool.Return(_buffers[i]);
            }

            _buffers.Clear();
        }

        // We keep current as it is faster for small responses. The Path Segment itself
        // should is part of the PathFactory and therefore it is pooled too.
        if (_current is not null)
        {
            _current.Reset();
        }
    }
}

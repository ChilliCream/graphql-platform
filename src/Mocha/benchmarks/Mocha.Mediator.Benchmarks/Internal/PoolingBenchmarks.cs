using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.ObjectPool;

namespace Mocha.Mediator.Benchmarks.Internal;

/// <summary>
/// Compares pooling strategies for the MediatorContext object.
/// Each benchmark rents an object, writes to it, reads back, and returns it.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PoolingBenchmarks
{
    private sealed class PooledObject
    {
        public object? Message;
        public Type? MessageType;
        public object? Result;

        public void Reset()
        {
            Message = null;
            MessageType = null;
            Result = null;
        }
    }

    private sealed class PooledObjectPolicy : PooledObjectPolicy<PooledObject>
    {
        public override PooledObject Create() => new();

        public override bool Return(PooledObject obj)
        {
            obj.Reset();
            return true;
        }
    }

    private ConcurrentQueue<PooledObject> _concurrentQueue = null!;
    private ObjectPool<PooledObject> _objectPool = null!;
    private ThreadLocal<PooledObject> _threadLocal = null!;
    private ObjectPool<PooledObject> _threadStaticFallbackPool = null!;

    [ThreadStatic]
    private static PooledObject? t_cached;

    private static readonly object s_message = "hello";
    private static readonly Type s_type = typeof(string);

    [GlobalSetup]
    public void Setup()
    {
        _concurrentQueue = new ConcurrentQueue<PooledObject>();
        _objectPool = new DefaultObjectPool<PooledObject>(new PooledObjectPolicy(), 64);
        _threadLocal = new ThreadLocal<PooledObject>(() => new PooledObject(), trackAllValues: false);
        _threadStaticFallbackPool = new DefaultObjectPool<PooledObject>(new PooledObjectPolicy(), 64);
        t_cached = null;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _threadLocal.Dispose();
    }

    [Benchmark(Baseline = true)]
    public object? NewEveryTime()
    {
        var obj = new PooledObject();
        obj.Message = s_message;
        obj.MessageType = s_type;
        var result = obj.Message;
        // No return - GC collects it
        return result;
    }

    [Benchmark]
    public object? ConcurrentQueue_Pool()
    {
        if (!_concurrentQueue.TryDequeue(out var obj))
        {
            obj = new PooledObject();
        }

        obj.Message = s_message;
        obj.MessageType = s_type;
        var result = obj.Message;

        obj.Reset();
        _concurrentQueue.Enqueue(obj);
        return result;
    }

    [Benchmark]
    public object? ObjectPool_Default()
    {
        var obj = _objectPool.Get();

        obj.Message = s_message;
        obj.MessageType = s_type;
        var result = obj.Message;

        _objectPool.Return(obj);
        return result;
    }

    [Benchmark]
    public object? ThreadLocal_Reuse()
    {
        var obj = _threadLocal.Value!;

        obj.Message = s_message;
        obj.MessageType = s_type;
        var result = obj.Message;

        obj.Reset();
        return result;
    }

    [Benchmark]
    public object? ThreadStatic_WithObjectPoolFallback()
    {
        var obj = t_cached;
        if (obj is not null)
        {
            t_cached = null;
        }
        else
        {
            obj = _threadStaticFallbackPool.Get();
        }

        obj.Message = s_message;
        obj.MessageType = s_type;
        var result = obj.Message;

        obj.Reset();
        if (t_cached is null)
        {
            t_cached = obj;
        }
        else
        {
            _threadStaticFallbackPool.Return(obj);
        }

        return result;
    }
}

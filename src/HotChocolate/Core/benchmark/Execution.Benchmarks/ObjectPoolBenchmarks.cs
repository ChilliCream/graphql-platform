using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

#nullable enable

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class ObjectPoolBenchmarks
    {
        private readonly int _poolSize = 8;

        [Params(200)]
        public int Size { get; set; }

        [Benchmark]
        public void BufferedObjectPoolCStack_GET_AND_RETURN_MANY()
        {
            var pool = new TestPool<ObjectBufferCStack<PoolElement>>(_poolSize);
            var buffer = new BufferedObjectPoolCStack<PoolElement>(pool);
            var stack = new Stack<PoolElement>();
            for (int i = 0; i < Size; i++)
            {
                stack.Push(buffer.Get());
                if (stack.Count > 30)
                {
                    while (stack.TryPop(out PoolElement element))
                    {
                        buffer.Return(element);
                    }
                }
            }
        }

        [Benchmark]
        public void BufferedObjectPoolBasicLock_GET_AND_RETURN_MANY()
        {
            var pool = new TestPool<ObjectBufferBasicLock<PoolElement>>(_poolSize);
            var buffer = new BufferedObjectPoolBasicLock<PoolElement>(pool);
            var stack = new Stack<PoolElement>();
            for (int i = 0; i < Size; i++)
            {
                stack.Push(buffer.Get());
                if (stack.Count > 30)
                {
                    while (stack.TryPop(out PoolElement element))
                    {
                        buffer.Return(element);
                    }
                }
            }
        }

        [Benchmark]
        public void BufferedObjectPoolNoLockInterlocked_GET_AND_RETURN_MANY()
        {
            var pool = new TestPool<ObjectBufferNoLockInterlocked<PoolElement>>(_poolSize);
            var buffer = new BufferedObjectPoolNoLockInterlocked<PoolElement>(pool);
            var stack = new Stack<PoolElement>();
            for (int i = 0; i < Size; i++)
            {
                stack.Push(buffer.Get());
                if (stack.Count > 30)
                {
                    while (stack.TryPop(out PoolElement element))
                    {
                        buffer.Return(element);
                    }
                }
            }
        }

        [Benchmark]
        public void ObjectPool_GET_AND_RETURN_MANY()
        {
            var pool = new TestPool<PoolElement>(_poolSize);
            var stack = new Stack<PoolElement>();
            for (int i = 0; i < Size; i++)
            {
                stack.Push(pool.Get());
                if (stack.Count > 30)
                {
                    while (stack.TryPop(out PoolElement element))
                    {
                        pool.Return(element);
                    }
                }
            }
        }

        [Benchmark]
        public void BufferedObjectPoolNoLock_GET_AND_RETURN_MANY()
        {
            var pool = new TestPool<ObjectBuffer<PoolElement>>(_poolSize);
            var buffer = new BufferedObjectPoolNoLock<PoolElement>(pool);
            var stack = new Stack<PoolElement>();
            for (int i = 0; i < Size; i++)
            {
                stack.Push(buffer.Get());
                if (stack.Count > 30)
                {
                    while (stack.TryPop(out PoolElement element))
                    {
                        buffer.Return(element);
                    }
                }
            }
        }

        [Benchmark]
        public void BufferedObjectPoolFullLock_GET_AND_RETURN_MANY()
        {
            var pool = new TestPool<ObjectBuffer<PoolElement>>(_poolSize);
            var buffer = new BufferedObjectPoolFullLock<PoolElement>(pool);
            var stack = new Stack<PoolElement>();
            for (int i = 0; i < Size; i++)
            {
                stack.Push(buffer.Get());
                if (stack.Count > 30)
                {
                    while (stack.TryPop(out PoolElement element))
                    {
                        buffer.Return(element);
                    }
                }
            }
        }

        public void BufferedObjectPoolBasicLock_GET_AND_RETURN()
        {
            var pool = new TestPool<ObjectBufferBasicLock<PoolElement>>(_poolSize);
            var buffer = new BufferedObjectPoolBasicLock<PoolElement>(pool);

            for (int i = 0; i < Size; i++)
            {
                buffer.Return(buffer.Get());
            }
        }

        public void BufferedObjectPoolBasicLock_GET()
        {
            var pool = new TestPool<ObjectBufferBasicLock<PoolElement>>(_poolSize);
            var buffer = new BufferedObjectPoolBasicLock<PoolElement>(pool);

            for (int i = 0; i < Size; i++)
            {
                buffer.Get();
            }
        }

        public void BufferedObjectPoolNoLock_GET()
        {
            var pool = new TestPool<ObjectBuffer<PoolElement>>(_poolSize);
            var buffer = new BufferedObjectPoolNoLock<PoolElement>(pool);

            for (int i = 0; i < Size; i++)
            {
                buffer.Get();
            }
        }

        public void BufferedObjectPoolNoLock_GET_AND_RETURN()
        {
            var pool = new TestPool<ObjectBuffer<PoolElement>>(_poolSize);
            var buffer = new BufferedObjectPoolNoLock<PoolElement>(pool);

            for (int i = 0; i < Size; i++)
            {
                buffer.Return(buffer.Get());
            }
        }

        public void ObjectPool_GET()
        {
            var pool = new TestPool<PoolElement>(_poolSize);

            for (int i = 0; i < Size; i++)
            {
                pool.Get();
            }
        }

        public void ObjectPool_GET_AND_RETURN()
        {
            var pool = new TestPool<PoolElement>(_poolSize);

            for (int i = 0; i < Size; i++)
            {
                pool.Return(pool.Get());
            }
        }

        private class PoolElement : IObjectBuffer
        {
            public void Reset()
            {
            }
        }

        private class TestPool<T> : DefaultObjectPool<T> where T : class, IObjectBuffer, new()
        {
            public List<T> Rented =
                new List<T>();

            public List<T> Returned =
                new List<T>();

            public TestPool(int size)
                : base(new Policy(), size)
            {
            }

            public override T Get()
            {
                T buffer = base.Get();
                Rented.Add(buffer);
                Returned.Remove(buffer);
                return buffer;
            }
            public override void Return(T obj)
            {
                Returned.Add(obj);
                Rented.Remove(obj);
                base.Return(obj);
            }

            private class Policy : IPooledObjectPolicy<T>
            {
                public T Create() => new T();

                public bool Return(T obj)
                {
                    obj.Reset();
                    return true;
                }
            }
        }
    }
}

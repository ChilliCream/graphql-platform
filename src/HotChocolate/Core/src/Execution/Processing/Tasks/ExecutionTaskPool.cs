using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Tasks
{
    /// <summary>
    ///  A pool of objects. Buffers a set of objects to ensure fast, thread safe object pooling
    /// </summary>
    internal sealed class ExecutionTaskPool<T> : ObjectPool<T> where T : class, IExecutionTask
    {
        private readonly ExecutionTaskPoolPolicy<T> _policy;
        private readonly T?[] _buffer;
        private readonly int _capacity;
        private int _index = -1;
        private int _sync;

        public ExecutionTaskPool(ExecutionTaskPoolPolicy<T> policy, int maximumRetained = 256)
        {
            _policy = policy;
            _buffer = new T?[maximumRetained];
            _capacity = maximumRetained - 1;
        }

        /// <summary>
        ///  Gets an object from the buffer if one is available, otherwise get a new buffer
        ///  from the pool one.
        /// </summary>
        /// <returns>A <see cref="ResolverTask"/>.</returns>
        public override T Get()
        {
            var current = Thread.CurrentThread.ManagedThreadId;
            T? resolverTask = null;
            SpinWait spin = default;

            while (true)
            {
                if (Interlocked.CompareExchange(ref _sync, current, 0) == 0)
                {
                    if (_index < _capacity)
                    {
                        resolverTask = _buffer[++_index];
                    }

                    Interlocked.Exchange(ref _sync, 0);
                    break;
                }

#if NETSTANDARD2_0
                spin.SpinOnce();
#else
                spin.SpinOnce(sleep1Threshold: -1);
#endif
            }

            resolverTask ??= _policy.Create(this);
            return resolverTask;
        }

        /// <summary>
        ///  Return an object from the buffer if one is available. If the buffer is full
        ///  return the buffer to the pool
        /// </summary>
        public override void Return(T obj)
        {
            if (_policy.Reset(obj))
            {
                var current = Thread.CurrentThread.ManagedThreadId;
                SpinWait spin = default;

                while (true)
                {
                    if (Interlocked.CompareExchange(ref _sync, current, 0) == 0)
                    {
                        if (_index > -1)
                        {
                            _buffer[_index--] = obj;
                        }
                        Interlocked.Exchange(ref _sync, 0);
                        break;
                    }

#if NETSTANDARD2_0
                    spin.SpinOnce();
#else
                    spin.SpinOnce(sleep1Threshold: -1);
#endif
                }
            }
        }
    }

    internal sealed class TaskBufferPoolPolicy : IPooledObjectPolicy<IExecutionTask?[]>
    {
        public IExecutionTask?[] Create()
        {
            return new IExecutionTask[4];
        }

        public bool Return(IExecutionTask?[] obj)
        {
            obj.AsSpan().Clear();
            return true;
        }
    }
}

using System.Threading;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    ///  A pool of objects. Buffers a set of objects to ensure fast, thread safe object pooling
    /// </summary>
    internal sealed class ResolverTaskPool : ObjectPool<ResolverTask>
    {
        private readonly ResolverTask?[] _buffer;
        private readonly int _capacity;
        private int _index = -1;
        private int _sync;

        public ResolverTaskPool(int maximumRetained = 256)
        {
            _buffer = new ResolverTask?[256];
            _capacity = maximumRetained - 1;
        }

        /// <summary>
        ///  Gets an object from the buffer if one is available, otherwise get a new buffer
        ///  from the pool one.
        /// </summary>
        /// <returns>A <see cref="ResolverTask"/>.</returns>
        public override ResolverTask Get()
        {
            var current = Thread.CurrentThread.ManagedThreadId;
            ResolverTask? resolverTask = null;
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

            resolverTask ??= new ResolverTask();
            return resolverTask;
        }

        /// <summary>
        ///  Return an object from the buffer if one is available. If the buffer is full
        ///  return the buffer to the pool
        /// </summary>
        public override void Return(ResolverTask obj)
        {
            if (obj.Reset())
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
}

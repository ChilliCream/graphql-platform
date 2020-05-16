using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using HotChocolate.Execution.Properties;

#nullable enable
namespace HotChocolate.Execution.Benchmarks
{
    internal sealed class ObjectBufferCStack<T> : IObjectBuffer where T : class, new()
    {
        private readonly object _objLock = new object();
        private readonly ConcurrentStack<T> _stack = new ConcurrentStack<T>();
        private readonly int _capacity;
        private readonly Action<T> _clean;
        private int _index;

        public ObjectBufferCStack() : this(8, x => { })
        {
        }

        public ObjectBufferCStack(int capacity, Action<T> clean)
        {
            _capacity = capacity;
            _clean = clean;
        }

        public T PopSafe()
        {
            if (TryPopSafe(out T? obj))
            {
                return obj;
            }
            throw new InvalidOperationException(Resources.ObjectBuffer_IsEmpty);
        }

        public bool TryPopSafe([NotNullWhen(true)] out T? obj)
        {
            if (_index < _capacity)
            {
                if (!_stack.TryPeek(out obj))
                {
                    obj = new T();
                }
                Interlocked.Increment(ref _index);
                return true;
            }

            obj = null;
            return false;
        }

        public void PushSafe(T obj)
        {
            if (!TryPushSafe(obj))
            {
                throw new InvalidOperationException(Resources.ObjectBuffer_IsUsedUp);
            }
        }

        public bool TryPushSafe(T obj)
        {
            if (0 < _index)
            {
                _clean(obj);
                _stack.Push(obj);
                Interlocked.Decrement(ref _index);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}

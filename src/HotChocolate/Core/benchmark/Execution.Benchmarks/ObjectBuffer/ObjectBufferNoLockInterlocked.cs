using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using HotChocolate.Execution.Properties;

#nullable enable
namespace HotChocolate.Execution.Benchmarks
{
    internal sealed class ObjectBufferNoLockInterlocked<T> : IObjectBuffer where T : class, new()
    {
        private readonly object _objLock = new object();
        private readonly T?[] _buffer;
        private readonly int _capacity;
        private readonly Action<T> _clean;
        private int _index;

        public ObjectBufferNoLockInterlocked() : this(8, x => { })
        {
        }

        public ObjectBufferNoLockInterlocked(int capacity, Action<T> clean)
        {
            _capacity = capacity;
            _buffer = new T[capacity];
            _clean = clean;
        }

        public T Pop()
        {
            if (TryPop(out T? obj))
            {
                return obj;
            }
            throw new InvalidOperationException("Resources.ObjectBuffer_IsEmpty");
        }

        public bool TryPop([NotNullWhen(true)] out T? obj)
        {
            if (_index < _capacity)
            {
                obj = _buffer[_index] ?? new T();
                _buffer[_index] = null;
                Interlocked.Increment(ref _index);
                return true;
            }

            obj = null;
            return false;
        }


        public void Push(T obj)
        {
            if (!TryPush(obj))
            {
                throw new InvalidOperationException("Resources.ObjectBuffer_IsUsedUp");
            }
        }

        public bool TryPush(T obj)
        {
            if (0 < _index)
            {
                _clean(obj);
                _buffer[Interlocked.Decrement(ref _index)] = obj;
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

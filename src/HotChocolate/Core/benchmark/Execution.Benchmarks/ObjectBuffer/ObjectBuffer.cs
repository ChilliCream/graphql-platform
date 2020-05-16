using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Properties;
#nullable enable
namespace HotChocolate.Execution.Benchmarks
{
    internal sealed class ObjectBuffer<T> : IObjectBuffer where T : class, new()
    {
        private readonly object _objLock = new object();
        private readonly T?[] _buffer;
        private readonly int _capacity;
        private readonly Action<T> _clean;
        private int _index;

        public ObjectBuffer() : this(8, x => { })
        {
        }

        public ObjectBuffer(int capacity, Action<T> clean)
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
                _buffer[_index++] = null;
                return true;
            }

            obj = null;
            return false;
        }

        public T PopSafe()
        {
            if (TryPopSafe(out T? obj))
            {
                return obj;
            }
            throw new InvalidOperationException("Resources.ObjectBuffer_IsEmpty");
        }

        public bool TryPopSafe([NotNullWhen(true)] out T? obj)
        {
            lock (_objLock)
            {
                return TryPop(out obj);
            }
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
                _buffer[--_index] = obj;
                return true;
            }
            return false;
        }

        public void PushSafe(T obj)
        {
            if (!TryPushSafe(obj))
            {
                throw new InvalidOperationException("Resources.ObjectBuffer_IsUsedUp");
            }
        }

        public bool TryPushSafe(T obj)
        {
            lock (_objLock)
            {
                return TryPush(obj);
            }
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}

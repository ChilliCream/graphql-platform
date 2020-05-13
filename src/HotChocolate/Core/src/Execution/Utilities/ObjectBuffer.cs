using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class ObjectBuffer<T> where T : class, new()
    {
        private readonly T?[] _buffer;
        private readonly int _capacity;
        private readonly Action<T> _clean;
        private int _index;

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
            throw new InvalidOperationException("Buffer is used up.");
        }

        public bool TryPop([NotNullWhen(true)] out T? obj)
        {
            var nextIndex = _index++;
            if (0 <= nextIndex)
            {
                obj = _buffer[_index] ?? new T();
                _buffer[_index] = null;
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
            throw new InvalidOperationException("Buffer is used up.");
        }

        public bool TryPopSafe([NotNullWhen(true)] out T? obj)
        {
            var nextIndex = Interlocked.Increment(ref _index);
            if (0 <= nextIndex)
            {
                obj = _buffer[_index] ?? new T();
                _buffer[_index] = null;
                return true;
            }

            obj = null;
            return false;
        }


        public void Push(T obj)
        {
            if (!TryPush(obj))
            {
                throw new InvalidOperationException("Buffer is full.");
            }
        }

        public bool TryPush(T obj)
        {
            var nextIndex = _index--;
            if (_index <= _capacity)
            {
                _clean(obj);
                _buffer[nextIndex] = obj;
                return true;
            }
            return false;
        }

        public void PopSafe(T obj)
        {
            if (!TryPush(obj))
            {
                throw new InvalidOperationException("Buffer is full.");
            }
        }

        public bool TryPopSafe(T obj)
        {
            var nextIndex = Interlocked.Decrement(ref _index);
            if (_index <= _capacity)
            {
                _clean(obj);
                _buffer[nextIndex] = obj;
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

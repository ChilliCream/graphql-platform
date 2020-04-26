using System;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class ObjectBuffer<T> where T : class, new()
    {
        private readonly T[] _buffer;
        private readonly int _capacity;
        private readonly Action<T> _clean;
        private int _index;

        public ObjectBuffer(int capacity, Action<T> clean)
        {
            _capacity = capacity;
            _buffer = new T[capacity];
            _clean = clean;

            for (var i = 0; i < _capacity; i++)
            {
                _buffer[i] = new T();
            }
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
            if (_index < _capacity)
            {
                obj = _buffer[_index++];
                return true;
            }

            obj = null;
            return false;
        }

        public void Reset()
        {
            if (_index > 0)
            {
                for (int i = 0; i < _index; i++)
                {
                    _clean(_buffer[i]);
                }
            }
            _index = 0;
        }
    }
}

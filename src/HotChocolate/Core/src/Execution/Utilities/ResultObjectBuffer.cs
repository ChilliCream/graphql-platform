using System;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class ResultObjectBuffer<T> where T : class, new()
    {
        private readonly T?[] _buffer;
        private readonly int _capacity;
        private readonly Action<T> _clean;
        private int _index;

        public ResultObjectBuffer(int capacity, Action<T> clean)
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
            if (nextIndex <= _capacity)
            {
                if (_buffer[nextIndex] is { } o)
                {
                    obj = o;
                    return true;
                }

                obj = new T();
                _buffer[nextIndex] = obj;
                return true;
            }

            obj = null;
            return false;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}

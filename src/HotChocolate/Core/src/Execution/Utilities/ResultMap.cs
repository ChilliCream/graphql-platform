using System;
using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public sealed class ResultMap : IResultMap, IReadOnlyDictionary<string, object?>
    {
        private static readonly ResultValue[] _empty = new ResultValue[0];
        private ResultValue[] _buffer;
        private int _capacity;
        private bool _needsDefrag = false;

        public ResultMap()
        {
            _buffer = _empty;
        }

        public IResultData? Parent { get; set; }

        public ResultValue this[int index] { get => _buffer[index]; }

        public int Count => _capacity;

        object? IReadOnlyDictionary<string, object?>.this[string key]
        {
            get
            {
                for (var i = 0; i < _capacity; i++)
                {
                    ResultValue value = _buffer[i];

                    if (value.HasValue && value.Name.Equals(key, StringComparison.Ordinal))
                    {
                        return value.Value;
                    }
                }

                throw new KeyNotFoundException(key);
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys
        {
            get
            {
                for (var i = 0; i < _capacity; i++)
                {
                    ResultValue value = _buffer[i];

                    if (value.HasValue)
                    {
                        yield return value.Name;
                    }
                }
            }
        }

        IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values
        {
            get
            {
                for (var i = 0; i < _capacity; i++)
                {
                    ResultValue value = _buffer[i];

                    if (value.HasValue)
                    {
                        yield return value.Value;
                    }
                }
            }
        }

        public void SetValue(int index, string name, object? value, bool isNullable = true)
        {
            if (index >= _capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            _buffer[index] = new ResultValue(name, value, isNullable);
        }

        public void RemoveValue(int index)
        {
            _needsDefrag = true;
            _buffer[index] = default;
        }

        public void Complete()
        {
            if (!_needsDefrag)
            {
                return;
            }

            var count = 0;

            for (var i = 0; i < _capacity; i++)
            {
                var moved = false;

                if (_buffer[i].HasValue)
                {
                    count = i + 1;
                }
                else
                {
                    for (var j = i + 1; j < _capacity; j++)
                    {
                        if (_buffer[j].HasValue)
                        {
                            _buffer[i] = _buffer[j];
                            moved = true;
                            break;
                        }
                    }

                    if (moved)
                    {
                        count = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            _capacity = count;
            _needsDefrag = false;
        }

        public void EnsureCapacity(int min)
        {
            if (_buffer.Length < min)
            {
                var newCapacity = _buffer.Length == 0 ? 4 : _buffer.Length * 2;
                if (newCapacity < min)
                {
                    newCapacity = min;
                }
                _buffer = new ResultValue[newCapacity];
            }
            _capacity = min;
            _needsDefrag = false;
        }

        public void Clear()
        {
            for (var i = 0; i < _capacity; i++)
            {
                _buffer[i] = default;
            }
            _needsDefrag = false;
        }

        bool IReadOnlyDictionary<string, object?>.ContainsKey(string key)
        {
            for (var i = 0; i < _capacity; i++)
            {
                ResultValue value = _buffer[i];

                if (value.HasValue && value.Name.Equals(key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        bool IReadOnlyDictionary<string, object?>.TryGetValue(string key, out object? value)
        {
            for (var i = 0; i < _capacity; i++)
            {
                ResultValue resultValue = _buffer[i];

                if (resultValue.HasValue && resultValue.Name.Equals(key, StringComparison.Ordinal))
                {
                    value = resultValue.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public IEnumerator<ResultValue> GetEnumerator()
        {
            for (var i = 0; i < _capacity; i++)
            {
                ResultValue value = _buffer[i];

                if (value.HasValue)
                {
                    yield return value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<KeyValuePair<string, object?>>
            IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
        {
            for (var i = 0; i < _capacity; i++)
            {
                ResultValue value = _buffer[i];

                if (value.HasValue)
                {
                    yield return new KeyValuePair<string, object?>(value.Name, value.Value);
                }
            }
        }
    }
}

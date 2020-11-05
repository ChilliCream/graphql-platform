using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing
{
    public sealed class ResultMap
        : IResultMap
        , IReadOnlyDictionary<string, object?>
        , IHasResultDataParent
    {
        private static readonly ResultValue[] _empty = new ResultValue[0];
        private ResultValue[] _buffer;
        private int _capacity;
        private bool _needsDefrag;

        public ResultMap()
        {
            _buffer = _empty;
        }

        public IResultData? Parent { get; set; }

        IResultData? IHasResultDataParent.Parent { get => Parent; set => Parent = value; }

        public ResultValue this[int index] { get => _buffer[index]; }

        public int Count => _capacity;

        object? IReadOnlyDictionary<string, object?>.this[string key]
        {
            get
            {
                ResultValue value = GetValue(key, out var index);
                if (index == -1)
                {
                    throw new KeyNotFoundException(key);
                }
                return value.Value;
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

        public ResultValue GetValue(string name, out int index)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var i = (IntPtr)0;
            var length = _capacity;
            ref ResultValue searchSpace = ref MemoryMarshal.GetReference(_buffer.AsSpan());

            // TODO : There is sometimes an issue with the last item of a batch
            /*
            while (length >= 8)
            {
                length -= 8;

                if (name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i += 0).Name) ||
                    name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i += 1).Name) ||
                    name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i += 2).Name) ||
                    name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i += 3).Name) ||
                    name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i += 4).Name) ||
                    name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i += 5).Name) ||
                    name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i += 6).Name) ||
                    name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i += 7).Name))
                {
                    index = i.ToInt32();
                    return _buffer[index];
                }

                i += 1;
            }

            if (length >= 4)
            {
                length -= 4;

                if (name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i += 0).Name) ||
                    name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i += 1).Name) ||
                    name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i += 2).Name) ||
                    name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i += 3).Name))
                {
                    index = i.ToInt32();
                    return _buffer[index];
                }

                i += 4;
            }
            */

            while (length > 0)
            {
                length -= 1;

                if (name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i).Name))
                {
                    index = i.ToInt32();
                    return _buffer[index];
                }

                i += 1;
            }

            index = -1;
            return default;
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

        public void EnsureCapacity(int capacity)
        {
            if (_buffer.Length < capacity)
            {
                var newCapacity = _buffer.Length == 0 ? 4 : _buffer.Length * 2;
                if (newCapacity < capacity)
                {
                    newCapacity = capacity;
                }
                _buffer = new ResultValue[newCapacity];
            }
            _capacity = capacity;
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

    internal interface IHasResultDataParent
    {
        IResultData? Parent { get; set; }
    }
}

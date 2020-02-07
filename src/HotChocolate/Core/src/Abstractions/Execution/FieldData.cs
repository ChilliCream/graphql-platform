using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Execution
{
    public class FieldData
        : IReadOnlyDictionary<string, object>
        , IEnumerable<FieldValue>
        , IDisposable
    {
        private static readonly ArrayPool<FieldValue> _pool =
            ArrayPool<FieldValue>.Create(1024, 64);
        private FieldValue[] _rented;
        private int _capacity;

        public FieldData(int fields)
        {
            _capacity = fields;
            _rented = _pool.Rent(fields);
        }

        public object GetFieldValue(int index)
        {
            if (_capacity <= index)
            {
                throw new ArgumentException(
                    "The specified index does not exist.",
                    nameof(index));
            }
            return _rented[index].Value;
        }

        public void SetFieldValue(int index, string key, object value)
        {
            if (_capacity <= index)
            {
                throw new ArgumentException(
                    "The specified index does not exist.",
                    nameof(index));
            }

            _rented[index] = new FieldValue(key, value);
        }

        public void Clear()
        {
            Dispose();
            _rented = Array.Empty<FieldValue>();
            _capacity = 0;
        }

        public void Dispose()
        {
            if (_rented.Length > 0)
            {
                for (var i = 0; i < _capacity; i++)
                {
                    DisposeNested(_rented[i].Value);
                }
                _pool.Return(_rented, true);
            }
        }

        private void DisposeNested(object o)
        {
            if (o is FieldData data)
            {
                data.Dispose();
            }
            else if (o is IList list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    DisposeNested(list[i]);
                }
            }
        }

        private bool TryGetFieldValue(string key, out FieldValue fieldValue)
        {
            fieldValue = Array.Find(
                _rented,
                t => string.Equals(t.Key, key, StringComparison.Ordinal));
            return fieldValue.HasValue;
        }

        object IReadOnlyDictionary<string, object>.this[string key]
        {
            get
            {
                if (TryGetFieldValue(key, out FieldValue value))
                {
                    return value.Value;
                }

                throw new KeyNotFoundException($"The key {key} does not exist.");
            }
        }

        bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value)
        {
            if (TryGetFieldValue(key, out FieldValue fieldValue))
            {
                value = fieldValue.Value;
                return true;
            }

            value = null;
            return false;
        }

        bool IReadOnlyDictionary<string, object>.ContainsKey(string key)
        {
            for (int i = 0; i < _capacity; i++)
            {
                FieldValue value = _rented[i];
                if (value.HasValue && string.Equals(value.Key, key, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys =>
            _rented.Take(_capacity)
                .Where(t => t.HasValue)
                .Select(t => t.Key);

        IEnumerable<object> IReadOnlyDictionary<string, object>.Values =>
            _rented.Take(_capacity)
                .Where(t => t.HasValue)
                .Select(t => t.Value);

        int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _capacity;

        public IEnumerator<FieldValue> GetEnumerator() =>
            _rented.Take(_capacity)
                .Where(t => t.HasValue)
                .GetEnumerator();

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() =>
            _rented.Take(_capacity)
                .Where(t => t.HasValue)
                .Select(t => new KeyValuePair<string, object>(t.Key, t.Value))
                .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>  GetEnumerator();
    }
}

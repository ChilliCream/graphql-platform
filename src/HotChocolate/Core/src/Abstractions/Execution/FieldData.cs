using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Execution
{
    public class FieldData
        : IReadOnlyDictionary<string, object>
        , IEnumerable<FieldValue>
    {
        private FieldValue[] _values;
        private int _count;

        public FieldData(int fields)
        {
            _values = new FieldValue[fields];
        }

        public object GetFieldValue(int index)
        {
            if (_values.Length <= index)
            {
                throw new ArgumentException(
                    "The specified index does not exist.",
                    nameof(index));
            }
            return _values[index].Value;
        }

        public void SetFieldValue(int index, string key, object value)
        {
            if (_values.Length <= index)
            {
                throw new ArgumentException(
                    "The specified index does not exist.",
                    nameof(index));
            }
            _values[index] = new FieldValue(key, value);
            _count++;
        }

        public void Clear()
        {
            _values = Array.Empty<FieldValue>();
            _count = 0;
        }

        private bool TryGetFieldValue(string key, out FieldValue fieldValue)
        {
            fieldValue = Array.Find(
                _values,
                t => string.Equals(t.Key, key, StringComparison.Ordinal));
            return !fieldValue.Equals(default(FieldValue));
        }

        object IReadOnlyDictionary<string, object>.this[string key]
        {
            get
            {
                if (TryGetFieldValue(key, out FieldValue value))
                {
                    return value;
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
            for (int i = 0; i < _values.Length; i++)
            {
                FieldValue value = _values[i];
                if (value.HasValue && string.Equals(value.Key, key, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys =>
            _values.Where(t => t.HasValue).Select(t => t.Key);

        IEnumerable<object> IReadOnlyDictionary<string, object>.Values =>
            _values.Where(t => t.HasValue).Select(t => t.Value);

        int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _count;

        public IEnumerator<FieldValue> GetEnumerator()
        {
            return _values.Where(t => t.HasValue).GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() =>
            _values.Where(t => t.HasValue).Select(t => new KeyValuePair<string, object>(t.Key, t.Value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

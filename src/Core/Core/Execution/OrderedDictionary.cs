using System;
using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public class OrderedDictionary
        : IOrderedDictionary
        , IDictionary<string, object>
    {
        private bool _isReadOnly;
        private readonly List<KeyValuePair<string, object>> _order = new List<KeyValuePair<string, object>>();
        private readonly Dictionary<string, object> _map = new Dictionary<string, object>();

        public object this[string key]
        {
            get
            {
                return _map[key];
            }
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(
                        "The dictionary is read-only.");
                }

                if (_map.ContainsKey(key))
                {
                    _map[key] = value;
                    _order[IndexOfKey(key)] =
                        new KeyValuePair<string, object>(key, value);
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        public ICollection<string> Keys => _map.Keys;

        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => Keys;

        public ICollection<object> Values => _map.Values;

        IEnumerable<object> IReadOnlyDictionary<string, object>.Values => Values;

        public int Count => _order.Count;

        public bool IsReadOnly => _isReadOnly;

        public void Add(string key, object value)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(
                    "The dictionary is read-only.");
            }

            _map.Add(key, value);
            _order.Add(new KeyValuePair<string, object>(key, value));
        }

        public void Add(KeyValuePair<string, object> item)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(
                    "The dictionary is read-only.");
            }

            _map.Add(item.Key, item.Value);
            _order.Add(item);
        }

        public void Clear()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(
                    "The dictionary is read-only.");
            }

            _map.Clear();
            _order.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return _order.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _map.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            _order.CopyTo(array, arrayIndex);
        }

        public bool Remove(string key)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(
                    "The dictionary is read-only.");
            }

            bool success = _map.Remove(key);
            _order.RemoveAt(IndexOfKey(key));
            return success;
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(
                    "The dictionary is read-only.");
            }

            int index = _order.IndexOf(item);
            if (index != -1)
            {
                _order.RemoveAt(index);
                _map.Remove(item.Key);
                return true;
            }
            return false;
        }

        public bool TryGetValue(string key, out object value)
        {
            return _map.TryGetValue(key, out value);
        }

        private int IndexOfKey(string key)
        {
            for (int i = 0; i < _order.Count; i++)
            {
                if (string.Equals(key, _order[i].Key, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return -1;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _order.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _order.GetEnumerator();
        }

        public void MakeReadOnly()
        {
            _isReadOnly = true;
        }
    }
}

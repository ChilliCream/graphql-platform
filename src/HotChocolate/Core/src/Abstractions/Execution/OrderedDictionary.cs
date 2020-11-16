using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public class OrderedDictionary
        : OrderedDictionary<string, object>
    {
    }

    public class OrderedDictionary<TKey, TValue>
        : IDictionary<TKey, TValue>
        , IReadOnlyDictionary<TKey, TValue>
    {
        private readonly List<KeyValuePair<TKey, TValue>> _order;
        private readonly Dictionary<TKey, TValue> _map;

        public OrderedDictionary()
        {
            _order = new List<KeyValuePair<TKey, TValue>>();
            _map = new Dictionary<TKey, TValue>();
        }

        public OrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> values)
        {
            if (values is null)
            {
                throw new System.ArgumentNullException(nameof(values));
            }

            _order = new List<KeyValuePair<TKey, TValue>>();
            _map = new Dictionary<TKey, TValue>();

            foreach (KeyValuePair<TKey, TValue> item in values)
            {
                _map.Add(item.Key, item.Value);
                _order.Add(item);
            }
        }

        private OrderedDictionary(OrderedDictionary<TKey, TValue> source)
        {
            if (source is null)
            {
                throw new System.ArgumentNullException(nameof(source));
            }

            _order = new List<KeyValuePair<TKey, TValue>>(source._order);
            _map = new Dictionary<TKey, TValue>(source._map);
        }

        public TValue this[TKey key]
        {
            get
            {
                return _map[key];
            }
            set
            {
                if (_map.ContainsKey(key))
                {
                    _map[key] = value;
                    _order[IndexOfKey(key)] =
                        new KeyValuePair<TKey, TValue>(key, value);
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        public ICollection<TKey> Keys => _map.Keys;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys =>
            Keys;

        public ICollection<TValue> Values => _map.Values;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values =>
            Values;

        public int Count => _order.Count;

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _map.Add(item.Key, item.Value);
            _order.Add(item);
        }

        public void Clear()
        {
            _map.Clear();
            _order.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _order.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return _map.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _order.CopyTo(array, arrayIndex);
        }

        public bool Remove(TKey key)
        {
            if (_map.ContainsKey(key))
            {
                _map.Remove(key);
                _order.RemoveAt(IndexOfKey(key));
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            int index = _order.IndexOf(item);
            if (index != -1)
            {
                _order.RemoveAt(index);
                _map.Remove(item.Key);
                return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _map.TryGetValue(key, out value);
        }

        private int IndexOfKey(TKey key)
        {
            for (int i = 0; i < _order.Count; i++)
            {
                if (key.Equals(_order[i].Key))
                {
                    return i;
                }
            }
            return -1;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _order.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _order.GetEnumerator();
        }

        public OrderedDictionary<TKey, TValue> Clone() =>
            new OrderedDictionary<TKey, TValue>(this);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using static HotChocolate.Properties.AbstractionResources;

#nullable enable

namespace HotChocolate
{
    public class ExtensionData
        : IDictionary<string, object?>
        , IReadOnlyDictionary<string, object?>
    {
        private ImmutableDictionary<string, object?> _dict =
            ImmutableDictionary<string, object?>.Empty;

        public ExtensionData()
        {
        }

        public ExtensionData(ExtensionData extensionData)
        {
            _dict = extensionData._dict;
        }

        public ExtensionData(IReadOnlyDictionary<string, object?> extensionData)
        {
            ImmutableDictionary<string, object?>.Builder builder =
                ImmutableDictionary.CreateBuilder<string, object?>();

            builder.AddRange(extensionData);

            _dict = builder.ToImmutableDictionary();
        }

        public object? this[string key]
        {
            get => _dict[key];
            set => _dict = _dict.SetItem(key, value);
        }

        object? IReadOnlyDictionary<string, object?>.this[string key] => _dict[key];

        public ICollection<string> Keys => new ExtensionDataKeyCollection(_dict);

        IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => _dict.Keys;

        public ICollection<object?> Values => new ExtensionDataValueCollection(_dict);

        IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => false;

        public void Add(string key, object? value)
        {
            _dict = _dict.Add(key, value);
        }

        public void Add(KeyValuePair<string, object?> item)
        {
            _dict = _dict.Add(item.Key, item.Value);
        }

        public void AddRange(IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            _dict = _dict.AddRange(pairs);
        }

        public bool Remove(string key)
        {
            var contains = _dict.ContainsKey(key);
            _dict = _dict.Remove(key);
            return contains;
        }

        public bool Remove(KeyValuePair<string, object?> item)
        {
            var contains = _dict.ContainsKey(item.Key);
            _dict = _dict.Remove(item.Key);
            return contains;
        }

        public bool TryGetValue(string key, out object? value) =>
            _dict.TryGetValue(key, out value);

        bool IReadOnlyDictionary<string, object?>.TryGetValue(string key, out object? value) =>
            TryGetValue(key, out value);

        public bool Contains(KeyValuePair<string, object?> item)
        {
            return _dict.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        bool IReadOnlyDictionary<string, object?>.ContainsKey(string key) =>
            _dict.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object?>>)_dict).CopyTo(array, arrayIndex);
        }

        public void Clear()
        {
            _dict = ImmutableDictionary<string, object?>.Empty;
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private sealed class ExtensionDataKeyCollection : ExtensionDataCollection<string>
        {
            private readonly ImmutableDictionary<string, object?> _dict;

            public ExtensionDataKeyCollection(ImmutableDictionary<string, object?> dict)
                : base(dict)
            {
                _dict = dict;
            }

            public override bool Contains(string item) =>
                _dict.ContainsKey(item);

            public override void CopyTo(string[] array, int arrayIndex)
            {
                if (array.Length - arrayIndex < _dict.Count)
                {
                    throw new ArgumentException(
                        ExtensionDataKeyCollection_CopyTo_ArrayNotBigEnough,
                        nameof(array));
                }

                var i = arrayIndex;

                foreach (string key in _dict.Keys)
                {
                    array[i++] = key;
                }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return _dict.Keys.GetEnumerator();
            }
        }

        private sealed class ExtensionDataValueCollection : ExtensionDataCollection<object?>
        {
            private readonly ImmutableDictionary<string, object?> _dict;

            public ExtensionDataValueCollection(ImmutableDictionary<string, object?> dict)
                : base(dict)
            {
                _dict = dict;
            }

            public override bool Contains(object? item) =>
                _dict.ContainsValue(item);

            public override void CopyTo(object?[] array, int arrayIndex)
            {
                if (array.Length - arrayIndex < _dict.Count)
                {
                    throw new ArgumentException(
                        ExtensionDataKeyCollection_CopyTo_ArrayNotBigEnough,
                        nameof(array));
                }

                var i = arrayIndex;

                foreach (var value in _dict.Values)
                {
                    array[i++] = value;
                }
            }

            public override IEnumerator<object?> GetEnumerator()
            {
                return _dict.Keys.GetEnumerator();
            }
        }

        private abstract class ExtensionDataCollection<T> : ICollection<T>
        {
            private readonly ImmutableDictionary<string, object?> _dict;

            protected ExtensionDataCollection(ImmutableDictionary<string, object?> dict)
            {
                _dict = dict;
            }

            public int Count => _dict.Count;

            public bool IsReadOnly => true;

            public void Add(T item)
            {
                throw new InvalidOperationException(ExtensionDataCollection_CollectionIsReadOnly);
            }

            public bool Remove(T item)
            {
                throw new InvalidOperationException(ExtensionDataCollection_CollectionIsReadOnly);
            }

            public void Clear()
            {
                throw new InvalidOperationException(ExtensionDataCollection_CollectionIsReadOnly);
            }

            public abstract bool Contains(T item);

            public abstract void CopyTo(T[] array, int arrayIndex);

            public abstract IEnumerator<T> GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public static readonly ExtensionData Empty = new();
    }
}

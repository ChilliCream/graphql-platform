using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public sealed class BindableList<T>
        : IBindableList<T>
    {
        private List<T>? _list;

        public BindingBehavior BindingBehavior { get; set; }

        public int Count => _list?.Count ?? 0;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            _list ??= new List<T>();
            _list.Add(item);
        }

        public void AddRange(IEnumerable<T> items)
        {
            _list ??= new List<T>();
            _list.AddRange(items);
        }

        public void Clear()
        {
            _list?.Clear();
            _list = null;
        }

        public bool Contains(T item)
        {
            return _list is not null && _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list?.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            if (_list is null)
            {
                return false;
            }

            var result = _list.Remove(item);

            if (_list.Count == 0)
            {
                _list = null;
            }

            return result;
        }

        public int IndexOf(T item)
        {
            if (_list is null)
            {
                return -1;
            }

            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _list ??= new List<T>();
            _list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _list?.RemoveAt(index);
        }

        public T this[int index]
        {
            get => _list is not null ? _list[index] : throw new ArgumentOutOfRangeException();
            set
            {
                _list ??= new List<T>();
                _list[index] = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_list is null)
            {
                return Enumerable.Empty<T>().GetEnumerator();
            }

            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

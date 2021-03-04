using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J
{
    public class SingletonList<T> : List<T>
    {
        private readonly T _item;

        public SingletonList(T item)
        {
            _item = item;
        }

        public new void Add(T item)
        {
            throw new NotSupportedException("Add not supported.");
        }

        public new void Clear()
        {
            throw new NotSupportedException("Clear not supported.");
        }

        public new bool Contains(T item)
        {
            if (item == null) return _item == null;

            return item.Equals(_item);
        }

        public new void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException("array");

            array[arrayIndex] = _item;
        }

        public new bool Remove(T item)
        {
            throw new NotSupportedException("Remove not supported.");
        }

        public new int Count
        {
            get { return 1; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public new int IndexOf(T item)
        {
            return Contains(item) ? 0 : -1;
        }

        public new void Insert(int index, T item)
        {
            throw new NotSupportedException("Insert not supported.");
        }

        public new void RemoveAt(int index)
        {
            throw new NotSupportedException("RemoveAt not supported.");
        }

        public new T this[int index]
        {
            get
            {
                if (index == 0) return _item;

                throw new IndexOutOfRangeException();
            }
            set { throw new NotSupportedException("Set not supported."); }
        }
    }
}

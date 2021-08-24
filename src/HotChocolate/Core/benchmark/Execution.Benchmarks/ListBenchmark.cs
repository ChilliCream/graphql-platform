using System;
using System.Collections;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class ListBenchmark
    {
        [Params(0, 1, 2, 3, 4)]
        public int Size { get; set; }

        [Benchmark]
        public List<int> ListOfInt()
        {
            var list = new List<int>();

            for (var i = 0; i < Size; i++)
            {
                list.Add(i);
            }

            return list;
        }

        [Benchmark]
        public List<string> ListOfString()
        {
            var list = new List<string>();

            for (var i = 0; i < Size; i++)
            {
                list.Add("abc");
            }

            return list;
        }

        [Benchmark]
        public OptimizedList<int> OptimizedListOfInt()
        {
            var list = new OptimizedList<int>();

            for (var i = 0; i < Size; i++)
            {
                list.Add(i);
            }

            return list;
        }

        [Benchmark]
        public OptimizedList<string> OptimizedListOfString()
        {
            var list = new OptimizedList<string>();

            for (var i = 0; i < Size; i++)
            {
                list.Add("abc");
            }

            return list;
        }

        [Benchmark]
        public OptimizedList2<int> OptimizedListOfInt2()
        {
            var list = new OptimizedList2<int>();

            for (var i = 0; i < Size; i++)
            {
                list.Add(i);
            }

            return list;
        }

        [Benchmark]
        public OptimizedList2<string> OptimizedListOfString2()
        {
            var list = new OptimizedList2<string>();

            for (var i = 0; i < Size; i++)
            {
                list.Add("abc");
            }

            return list;
        }

        [Benchmark]
        public OptimizedList3<int> OptimizedListOfInt3()
        {
            var list = new OptimizedList3<int>();

            for (var i = 0; i < Size; i++)
            {
                list.Add(i);
            }

            return list;
        }

        [Benchmark]
        public OptimizedList3<string> OptimizedListOfString3()
        {
            var list = new OptimizedList3<string>();

            for (var i = 0; i < Size; i++)
            {
                list.Add("abc");
            }

            return list;
        }

        [Benchmark]
        public OptimizedList4<int> OptimizedListOfInt4()
        {
            var list = new OptimizedList4<int>();

            for (var i = 0; i < Size; i++)
            {
                list.Add(i);
            }

            return list;
        }

        [Benchmark]
        public OptimizedList4<string> OptimizedListOfString4()
        {
            var list = new OptimizedList4<string>();

            for (var i = 0; i < Size; i++)
            {
                list.Add("abc");
            }

            return list;
        }

        [Benchmark]
        public ListList<int> ListListOfInt()
        {
            var list = new ListList<int>();

            for (var i = 0; i < Size; i++)
            {
                list.Add(i);
            }

            return list;
        }

        [Benchmark]
        public ListList<string> ListListOfString4()
        {
            var list = new ListList<string>();

            for (var i = 0; i < Size; i++)
            {
                list.Add("abc");
            }

            return list;
        }
    }

    public class OptimizedList<T> : IList<T>
    {
        private T _first;
        private T _second;
        private T[] _items;
        private int _count;

        public T this[int index] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public int Count => _count;

        public bool IsReadOnly => throw new System.NotImplementedException();

        public void Add(T item)
        {
            if (_count == 0)
            {
                _first = item;
                _count++;
            }
            else if (_count == 1)
            {
                _second = item;
                _count++;
            }
            else
            {
                EnsureCapacity();
                _items[_count] = item;
                _count++;
            }
        }

        private void EnsureCapacity()
        {
            if (_count == 2 && _items is null)
            {
                _items = new T[4];
                _items[0] = _first;
                _items[1] = _second;
                _first = default;
                _second = default;
            }

            if(_count > 1 && _items is not null && _items.Length == _count)
            {
                Array.Resize(ref _items, _count * 2);
            }
        }

        public void Clear()
        {

        }

        public bool Contains(T item)
        {
            throw new System.NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        public int IndexOf(T item)
        {
            throw new System.NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new System.NotImplementedException();
        }
    }

    public class OptimizedList2<T> : IList<T>
    {
        private T _first;
        private T[] _items;
        private int _count;

        public T this[int index] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public int Count => _count;

        public bool IsReadOnly => throw new System.NotImplementedException();

        public void Add(T item)
        {
            if (_count == 0)
            {
                _first = item;
                _count++;
            }
            else
            {
                EnsureCapacity();
                _items[_count] = item;
                _count++;
            }
        }

        private void EnsureCapacity()
        {
            if (_count == 1 && _items is null)
            {
                _items = new T[4];
                _items[0] = _first;
                _first = default;
            }

            if(_count > 0 && _items is not null && _items.Length == _count)
            {
                Array.Resize(ref _items, _count * 2);
            }
        }

        public void Clear()
        {

        }

        public bool Contains(T item)
        {
            throw new System.NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        public int IndexOf(T item)
        {
            throw new System.NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new System.NotImplementedException();
        }
    }

     public class OptimizedList3<T> : IList<T>
    {
        private T[] _items = new T[0];
        private int _count;

        public T this[int index] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public int Count => _count;

        public bool IsReadOnly => throw new System.NotImplementedException();

        public void Add(T item)
        {
            EnsureCapacity();
            _items[_count] = item;
            _count++;
        }

        private void EnsureCapacity()
        {
            if(_items.Length == _count)
            {
                Array.Resize(ref _items, _count * 2);
            }
        }

        public void Clear()
        {

        }

        public bool Contains(T item)
        {
            throw new System.NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        public int IndexOf(T item)
        {
            throw new System.NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new System.NotImplementedException();
        }
    }

      public class OptimizedList4<T> : IList<T>
    {
        private Entry _first;
        private Entry _second;
        private T[] _items;
        private int _count;

        public T this[int index] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public int Count => _count;

        public bool IsReadOnly => throw new System.NotImplementedException();

        public void Add(T item)
        {
            if (_count == 0)
            {
                _first = new Entry { Item = item };
                _count++;
            }
            else if (_count == 1)
            {
                _second = new Entry { Item = item };
                _count++;
            }
            else
            {
                EnsureCapacity();
                _items[_count] = item;
                _count++;
            }
        }

        private void EnsureCapacity()
        {
            if (_count == 2 && _items is null)
            {
                _items = new T[4];
                _items[0] = _first.Item;
                _items[1] = _second.Item;
                _first = null;
                _second = null;
            }

            if(_count > 1 && _items is not null && _items.Length == _count)
            {
                Array.Resize(ref _items, _count * 2);
            }
        }

        public void Clear()
        {

        }

        public bool Contains(T item)
        {
            throw new System.NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        public int IndexOf(T item)
        {
            throw new System.NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        private class Entry
        {
            public T Item { get; set; }
        }
    }

    public class ListList<T> : IList<T>
    {
        private List<T> _items;

        public T this[int index] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public int Count => _items?.Count ?? 0;

        public bool IsReadOnly => throw new System.NotImplementedException();

        public void Add(T item)
        {
            (_items ??= new List<T>()).Add(item);
        }

        public void Clear()
        {

        }

        public bool Contains(T item)
        {
            throw new System.NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        public int IndexOf(T item)
        {
            throw new System.NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new System.NotImplementedException();
        }
    }
}

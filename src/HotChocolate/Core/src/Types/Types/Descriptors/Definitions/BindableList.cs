using System.Collections;
using System.Runtime.InteropServices;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

public sealed class BindableList<T> : IBindableList<T>
{
    private static readonly T[] _empty = [];

    private List<T>? _list;

    public BindingBehavior BindingBehavior { get; set; }

    public int Count => _list?.Count ?? 0;

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        _list ??= [];
        _list.Add(item);
    }

    public void AddRange(IEnumerable<T> items)
    {
        _list ??= [];
        _list.AddRange(items);
    }

    public void Clear()
    {
        _list?.Clear();
        _list = null;
    }

    public bool Contains(T item)
        => _list is not null && _list.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
        => _list?.CopyTo(array, arrayIndex);

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
        _list ??= [];
        _list.Insert(index, item);
    }

    public void RemoveAt(int index)
        => _list?.RemoveAt(index);

    public T this[int index]
    {
        get
        {
            return _list is not null
                ? _list[index]
                : throw new IndexOutOfRangeException();
        }

        set
        {
            _list ??= [];
            _list[index] = value;
        }
    }

    internal ReadOnlySpan<T> AsSpan()
    {
        if (_list is null)
        {
            return _empty;
        }

        return CollectionsMarshal.AsSpan(_list);
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
        => GetEnumerator();
}

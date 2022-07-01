using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HotChocolate.Execution.Processing;

public abstract class ListResultBase<T> : ResultData, IReadOnlyList<T?>
{
    private T?[] _buffer = new T?[4];
    private int _capacity;
    private int _count;

    public int Capacity => _capacity;

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count"/>
    public int Count => _count;

    /// <inheritdoc cref="IReadOnlyList{T}.this"/>
    public T? this[int index]
    {
        get
        {
            return _buffer[index];
        }
    }

    /// <summary>
    /// Defines if the elements of this list are nullable.
    /// </summary>
    internal bool IsNullable { get; set; }

    internal void AddUnsafe(T? item)
        => _buffer[_count++] = item;

    internal void SetUnsafe(int index, T? item)
        => _buffer[index] = item;

    /// <summary>
    /// Ensures that the result object has enough capacity on the buffer
    /// to store the expected fields.
    /// </summary>
    /// <param name="capacity">
    /// The capacity needed.
    /// </param>
    internal void EnsureCapacity(int capacity)
    {
        if (_capacity > 0)
        {
            Reset();
        }

        if (_buffer.Length < capacity)
        {
            var newCapacity = _buffer.Length * 2;

            if (newCapacity < capacity)
            {
                newCapacity = capacity;
            }

            _capacity = newCapacity;

            Array.Resize(ref _buffer, newCapacity);
        }

        _capacity = capacity;
    }

    /// <summary>
    /// Grows the internal capacity.
    /// </summary>
    internal void Grow()
    {
        if (_capacity == 0)
        {
            EnsureCapacity(4);
            return;
        }

        var newCapacity = _capacity * 2;
        Array.Resize(ref _buffer, newCapacity);
        _capacity = newCapacity;
    }

    /// <summary>
    /// Resets the result object.
    /// </summary>
    internal void Reset()
    {
        if (_capacity > 0)
        {
            _buffer.AsSpan().Slice(0, _capacity).Clear();
            _capacity = 0;
            _count = 0;
        }
    }

    internal ref T? GetReference()
        => ref MemoryMarshal.GetReference(_buffer.AsSpan());

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public IEnumerator<T?> GetEnumerator()
    {
        for (var i = 0; i < _capacity; i++)
        {
            yield return _buffer[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

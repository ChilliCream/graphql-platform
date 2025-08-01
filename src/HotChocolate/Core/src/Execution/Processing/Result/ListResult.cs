using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents an optimized list result that is used by the execution engine
/// to store completed elements.
/// </summary>
public sealed class ListResult : ResultData, IReadOnlyList<object?>
{
    private object?[] _buffer = [];
    private int _capacity;
    private int _count;

    /// <summary>
    /// Gets the number of elements this list can hold.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the number of elements in this list.
    /// </summary>
    public int Count => _count;

    /// <inheritdoc cref="IReadOnlyList{T}.this"/>
    public object? this[int index] => _buffer[index];

    /// <summary>
    /// Defines if the elements of this list are nullable.
    /// </summary>
    internal bool IsNullable { get; set; }

    internal int AddUnsafe(object? item)
    {
        var index = _count++;
        _buffer[index] = item;
        return index;
    }

    internal int AddUnsafe(ResultData? item)
    {
        var index = _count++;
        item?.SetParent(this, index);
        _buffer[index] = item;
        return index;
    }

    internal void SetUnsafe(int index, object? item)
    {
        _buffer[index] = item;
    }

    internal void SetUnsafe(int index, ResultData? item)
    {
        item?.SetParent(this, index);
        _buffer[index] = item;
    }

    internal bool TrySetNull(int index)
    {
        if (_count > index)
        {
            _buffer[index] = null;
            return IsNullable;
        }

        return false;
    }

    /// <summary>
    /// Ensures that the result object has enough capacity on the buffer
    /// to store the expected fields.
    /// </summary>
    /// <param name="requiredCapacity">
    /// The capacity needed.
    /// </param>
    internal void EnsureCapacity(int requiredCapacity)
    {
        // If this list has a capacity specified we will reset it.
        // The capacity is only set when the list is rented out,
        // Once the item is returned the capacity is reset to zero.
        if (_capacity > 0)
        {
            Reset();
        }

        if (_buffer.Length < requiredCapacity)
        {
            Array.Resize(ref _buffer, requiredCapacity);
        }

        _capacity = requiredCapacity;
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

    public override void WriteTo(
        Utf8JsonWriter writer,
        JsonSerializerOptions? options = null,
        JsonNullIgnoreCondition nullIgnoreCondition = JsonNullIgnoreCondition.None)
    {
#if NET9_0_OR_GREATER
        options ??= JsonSerializerOptions.Web;
#else
        options ??= JsonSerializerOptions.Default;
#endif

        writer.WriteStartArray();

        ref var item = ref GetReference();
        ref var end = ref Unsafe.Add(ref item, _count);

        while (Unsafe.IsAddressLessThan(ref item, ref end))
        {
            if (item is null)
            {
                if ((nullIgnoreCondition & JsonNullIgnoreCondition.Lists) != JsonNullIgnoreCondition.Lists)
                {
                    writer.WriteNullValue();
                }
            }
            else
            {
                if (item is ResultData resultData)
                {
                    resultData.WriteTo(writer, options, nullIgnoreCondition);
                }
                else
                {
                    JsonValueFormatter.WriteValue(writer, item, options, nullIgnoreCondition);
                }
            }

            item = ref Unsafe.Add(ref item, 1)!;
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Resets the result object.
    /// </summary>
    internal void Reset()
    {
        if (_capacity > 0)
        {
            _buffer.AsSpan()[.._capacity].Clear();
            _capacity = 0;
            _count = 0;
        }

        IsInvalidated = false;
        ParentIndex = 0;
        Parent = null;
        PatchId = 0;
        PatchPath = null;
    }

    private ref object? GetReference()
        => ref MemoryMarshal.GetReference(_buffer.AsSpan());

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public IEnumerator<object?> GetEnumerator()
    {
        for (var i = 0; i < _count; i++)
        {
            yield return _buffer[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

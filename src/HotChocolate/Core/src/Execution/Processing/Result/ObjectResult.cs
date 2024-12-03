using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents an optimized object result that is used by the execution engine
/// to store completed values.
/// </summary>
public sealed class ObjectResult
    : ResultData
    , IReadOnlyDictionary<string, object?>
    , IEnumerable<ObjectFieldResult>
{
    private ObjectFieldResult[] _buffer = [];
    private int _capacity;

    /// <summary>
    /// Gets the capacity of this object result.
    /// It essentially specifies how many field results can be stored.
    /// </summary>
    internal int Capacity => _capacity;

    /// <summary>
    /// This indexer allows direct access to the underlying buffer
    /// to access a <see cref="ObjectFieldResult"/>.
    /// </summary>
    internal ObjectFieldResult this[int index] => _buffer[index];

    /// <summary>
    /// Gets a reference to the first <see cref="ObjectFieldResult"/> in the buffer.
    /// </summary>
    internal ref ObjectFieldResult GetReference()
        => ref MemoryMarshal.GetReference(_buffer.AsSpan());

    /// <summary>
    /// Sets a field value in the buffer.
    /// Note: Set will not validate if the buffer has enough space.
    /// </summary>
    /// <param name="index">
    /// The index in the buffer on which the value shall be stored.
    /// </param>
    /// <param name="name">
    /// The name of the field.
    /// </param>
    /// <param name="value">
    /// The field value.
    /// </param>
    /// <param name="isNullable">
    /// Specifies if the value is allowed to be null.
    /// </param>
    internal void SetValueUnsafe(int index, string name, object? value, bool isNullable = true)
        => _buffer[index].Set(name, value, isNullable);

    /// <summary>
    /// Sets a field value in the buffer.
    /// Note: Set will not validate if the buffer has enough space.
    /// </summary>
    /// <param name="index">
    /// The index in the buffer on which the value shall be stored.
    /// </param>
    /// <param name="name">
    /// The name of the field.
    /// </param>
    /// <param name="value">
    /// The field value.
    /// </param>
    /// <param name="isNullable">
    /// Specifies if the value is allowed to be null.
    /// </param>
    internal void SetValueUnsafe(int index, string name, ResultData? value, bool isNullable = true)
    {
        value?.SetParent(this, index);
        _buffer[index].Set(name, value, isNullable);
    }

    /// <summary>
    /// Removes a field value from the buffer.
    /// Note: Remove will not validate if the buffer has enough space.
    /// </summary>
    /// <param name="index">
    /// The index in the buffer on which the value shall be removed.
    /// </param>
    internal void RemoveValueUnsafe(int index)
    {
        _buffer[index].Reset();
    }

    /// <summary>
    /// Searches within the capacity of the buffer to find a field value that matches
    /// the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the field to search for.
    /// </param>
    /// <param name="index">
    /// The index on the buffer where the field value is located.
    /// </param>
    /// <returns>
    /// Returns the field value or null.
    /// </returns>
    internal ObjectFieldResult? TryGetValue(string name, out int index)
    {
        ref var searchSpace = ref GetReference();

        for(var i = 0; i < _capacity; i++)
        {
            var item = Unsafe.Add(ref searchSpace, i);
            if (name.EqualsOrdinal(item.Name))
            {
                index = i;
                return item;
            }
        }

        index = -1;
        return default;
    }

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
            var oldCapacity = _buffer.Length;
            Array.Resize(ref _buffer, capacity);

            for (var i = oldCapacity; i < _buffer.Length; i++)
            {
                var field = new ObjectFieldResult();
                _buffer[i] = field;
            }
        }

        _capacity = capacity;
    }

    /// <summary>
    /// Resets the result object.
    /// </summary>
    internal void Reset()
    {
        ref var searchSpace = ref GetReference();

        for(var i = 0; i < _capacity; i++)
        {
            Unsafe.Add(ref searchSpace, i).Reset();
        }

        _capacity = 0;
        IsInvalidated = false;
        ParentIndex = 0;
        Parent = null;
        PatchId = 0;
        PatchPath = null;
    }

    object? IReadOnlyDictionary<string, object?>.this[string key]
        => TryGetValue(key, out _)?.Value;

    IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys
    {
        get
        {
            for (var i = 0; i < _capacity; i++)
            {
                var fieldResult = _buffer[i];

                if (fieldResult.IsInitialized)
                {
                    yield return fieldResult.Name;
                }
            }
        }
    }

    IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values
    {
        get
        {
            for (var i = 0; i < _capacity; i++)
            {
                var fieldResult = _buffer[i];

                if (fieldResult.IsInitialized)
                {
                    yield return fieldResult.Value;
                }
            }
        }
    }

    int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => _capacity;

    bool IReadOnlyDictionary<string, object?>.ContainsKey(string key)
        => TryGetValue(key, out _)?.Name is not null;

    bool IReadOnlyDictionary<string, object?>.TryGetValue(string key, out object? value)
    {
        var field = TryGetValue(key, out _);

        if (field?.Name is not null)
        {
            value = field.Value;
            return true;
        }

        value = null;
        return false;
    }

    public IEnumerator<ObjectFieldResult> GetEnumerator()
    {
        for (var i = 0; i < _capacity; i++)
        {
            var field = _buffer[i];

            if (field.IsInitialized)
            {
                yield return field;
            }
        }
    }

    IEnumerator<KeyValuePair<string, object?>>
        IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
        for (var i = 0; i < _capacity; i++)
        {
            var field = _buffer[i];

            if (field.IsInitialized)
            {
                yield return new KeyValuePair<string, object?>(field.Name, field.Value);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal static class ObjectResultExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InitValueUnsafe(this ObjectResult result, int index, ISelection selection)
        => result.SetValueUnsafe(index, selection.ResponseName, null, selection.Type.Kind is not TypeKind.NonNull);
}

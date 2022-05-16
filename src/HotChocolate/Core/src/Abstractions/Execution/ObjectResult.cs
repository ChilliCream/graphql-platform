using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Execution;

public sealed class ObjectResult
    : IResultData
    , IReadOnlyDictionary<string, object?>
    , IEnumerable<ObjectFieldResult>
{
    private ObjectFieldResult[] _buffer = { new(), new(), new(), new() };
    private int _capacity;

    public IResultData? Parent { get; internal set; }

    public int Capacity => _capacity;

    public ObjectFieldResult this[int index]
    {
        get
        {
            if (index > 0 && index < _capacity)
            {
                return _buffer[index];
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    internal ref ObjectFieldResult GetReference()
        => ref MemoryMarshal.GetReference(_buffer.AsSpan());

    internal void SetValueUnsafe(int index, string name, object? value, bool isNullable = true)
        => _buffer[index].Set(name, value, isNullable);

    internal void RemoveValueUnsafe(int index)
        => _buffer[index].Reset();

    internal ObjectFieldResult? TryGetValue(string name, out int index)
    {
        var i = (IntPtr)0;
        var length = _capacity;
        ref ObjectFieldResult searchSpace = ref MemoryMarshal.GetReference(_buffer.AsSpan());

        while (length > 0)
        {
            var otherName = Unsafe.Add(ref searchSpace, i).Name;
            if (name.Equals(otherName, StringComparison.Ordinal))
            {
                index = i.ToInt32();
                return _buffer[index];
            }

            length--;
            i += 1;
        }

        index = -1;
        return default;
    }

    internal void EnsureCapacity(int capacity)
    {
        if (_capacity > 0)
        {
            Reset();
        }

        if (_buffer.Length < capacity)
        {
            var oldCapacity = _buffer.Length;
            var newCapacity = _buffer.Length * 2;

            if (newCapacity < capacity)
            {
                newCapacity = capacity;
            }

            Array.Resize(ref _buffer, newCapacity);

            for (var i = oldCapacity; i < _buffer.Length; i++)
            {
                _buffer[i] = new();
            }
        }

        _capacity = capacity;
    }

    internal void Reset()
    {
        if (_capacity > 4)
        {
            var i = (IntPtr)0;
            var length = _capacity;
            ref ObjectFieldResult searchSpace = ref MemoryMarshal.GetReference(_buffer.AsSpan());

            while (length > 0)
            {
                Unsafe.Add(ref searchSpace, i).Reset();
                length--;
                i += 1;
            }
        }
        else
        {
            for (var j = _capacity - 1; j >= 0; j--)
            {
                _buffer[j].Reset();
            }
        }

        _capacity = 0;
    }

    object? IReadOnlyDictionary<string, object?>.this[string key]
        => TryGetValue(key, out _)?.Value;

    IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys
    {
        get
        {
            for (var i = 0; i < _capacity; i++)
            {
                ObjectFieldResult field = _buffer[i];

                if (field.IsInitialized)
                {
                    yield return field.Name;
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
                ObjectFieldResult field = _buffer[i];

                if (field.IsInitialized)
                {
                    yield return field.Value;
                }
            }
        }
    }

    int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => _capacity;

    bool IReadOnlyDictionary<string, object?>.ContainsKey(string key)
        => TryGetValue(key, out _)?.Name is not null;

    bool IReadOnlyDictionary<string, object?>.TryGetValue(string key, out object? value)
    {
        ObjectFieldResult? field = TryGetValue(key, out _);

        if(field?.Name is not null)
        {
            value = field.Value;
            return true;
        }

        value = null;
        return true;
    }

    public IEnumerator<ObjectFieldResult> GetEnumerator()
    {
        for (var i = 0; i < _capacity; i++)
        {
            ObjectFieldResult field = _buffer[i];

            if (field.IsInitialized)
            {
                yield return field;
            }
        }
    }

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
        for (var i = 0; i < _capacity; i++)
        {
            ObjectFieldResult field = _buffer[i];

            if (field.IsInitialized)
            {
                yield return new KeyValuePair<string, object?>(field.Name, field.Value);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

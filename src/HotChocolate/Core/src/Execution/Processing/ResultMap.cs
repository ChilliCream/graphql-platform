using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

public sealed class ResultMap
    : IResultMap
    , IReadOnlyDictionary<string, object?>
    , IHasResultDataParent
{
    private ResultValue[] _buffer;
    private int _capacity;

    public ResultMap()
    {
        _buffer = Array.Empty<ResultValue>();
    }

    public IResultData? Parent { get; set; }

    IResultData? IHasResultDataParent.Parent { get => Parent; set => Parent = value; }

    public ResultValue this[int index] { get => _buffer[index]; }

    public int Count => _capacity;

    object? IReadOnlyDictionary<string, object?>.this[string key]
    {
        get
        {
            ResultValue value = GetValue(key, out var index);

            if (index == -1)
            {
                throw new KeyNotFoundException(key);
            }

            return value.Value;
        }
    }

    IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys
    {
        get
        {
            for (var i = 0; i < _capacity; i++)
            {
                ResultValue value = _buffer[i];

                if (value.IsInitialized)
                {
                    yield return value.Name;
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
                ResultValue value = _buffer[i];

                if (value.IsInitialized)
                {
                    yield return value.Value;
                }
            }
        }
    }

    public void SetValue(int index, string name, object? value, bool isNullable = true)
    {
        if (index >= _capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        _buffer[index] = new ResultValue(name, value, isNullable);
    }

    public ResultValue GetValue(string name, out int index)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        var i = (IntPtr)0;
        var length = _capacity;
        ref ResultValue searchSpace = ref MemoryMarshal.GetReference(_buffer.AsSpan());

        while (length > 0)
        {
            length -= 1;

            if (name.EqualsOrdinal(Unsafe.Add(ref searchSpace, i).Name))
            {
                index = i.ToInt32();
                return _buffer[index];
            }

            i += 1;
        }

        index = -1;
        return default;
    }

    public void RemoveValue(int index) => _buffer[index] = default;

    public void EnsureCapacity(int capacity)
    {
        if (_buffer.Length < capacity)
        {
            var newCapacity = _buffer.Length is 0 ? 4 : _buffer.Length * 2;

            if (newCapacity < capacity)
            {
                newCapacity = capacity;
            }

            _buffer = new ResultValue[newCapacity];
        }

        _capacity = capacity;
    }

    public void Clear() => Array.Clear(_buffer, 0, _buffer.Length);

    bool IReadOnlyDictionary<string, object?>.ContainsKey(string key)
    {
        GetValue(key, out var index);
        return index is not -1;
    }

    bool IReadOnlyDictionary<string, object?>.TryGetValue(string key, out object? value)
    {
        value = GetValue(key, out var index).Value;
        return index is not -1;
    }

    public IEnumerator<ResultValue> GetEnumerator()
    {
        for (var i = 0; i < _capacity; i++)
        {
            ResultValue value = _buffer[i];

            if (value.IsInitialized)
            {
                yield return value;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<KeyValuePair<string, object?>>
        IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
        for (var i = 0; i < _capacity; i++)
        {
            ResultValue value = _buffer[i];

            if (value.IsInitialized)
            {
                yield return new KeyValuePair<string, object?>(value.Name, value.Value);
            }
        }
    }
}

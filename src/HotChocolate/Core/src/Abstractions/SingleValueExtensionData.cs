using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Execution;
using static HotChocolate.Properties.AbstractionResources;

namespace HotChocolate;

/// <summary>
/// An optimized extension data dictionary for <see cref="IOperationResult.Extensions"/> or
/// <see cref="IExecutionResult.ContextData"/> when only one value is needed.
/// </summary>
public sealed class SingleValueExtensionData : IReadOnlyDictionary<string, object?>
{
    private readonly string _key;
    private readonly object? _value;

    /// <summary>
    /// Creates a new instance of <see cref="SingleValueExtensionData"/>.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public SingleValueExtensionData(string key, object? value)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException(SingleValueExtensionData_KeyIsEmpty, nameof(key));
        }

        _key = key;
        _value = value;
    }

    /// <inheritdoc />
    public int Count => 1;

    /// <inheritdoc />
    public bool ContainsKey(string key) => string.Equals(_key, key, StringComparison.Ordinal);

    /// <inheritdoc />
    public bool TryGetValue(string key, out object? value)
    {
        if (ContainsKey(key))
        {
            value = _value;
            return true;
        }

        value = null;
        return false;
    }

    /// <inheritdoc />
    public object? this[string key]
    {
        get
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException(string.Format(
                SingleValueExtensionData_KeyNotFound,
                key));
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> Keys
    {
        get
        {
            yield return _key;
        }
    }

    /// <inheritdoc />
    public IEnumerable<object?> Values
    {
        get
        {
            yield return _value;
        }
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        yield return new KeyValuePair<string, object?>(_key, _value);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}

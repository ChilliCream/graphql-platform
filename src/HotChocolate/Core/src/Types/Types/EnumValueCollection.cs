using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents a collection of enum values.
/// </summary>
public sealed class EnumValueCollection : IReadOnlyList<EnumValue>, IReadOnlyDictionary<string, EnumValue>
{
    private readonly EnumValue[] _values;
    private readonly FrozenDictionary<string, EnumValue> _nameLookup;
    private ReadOnlyEnumValueCollection? _readOnlyValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumValueCollection"/> class.
    /// </summary>
    /// <param name="values">
    /// The enum values that are part of this collection.
    /// </param>
    public EnumValueCollection(EnumValue[] values)
    {
        _values = values;
        _nameLookup = _values.ToFrozenDictionary(v => v.Name, StringComparer.Ordinal);
    }

    public EnumValue this[string name] => _nameLookup[name];

    public EnumValue this[int index] => _values[index];

    public int Count => _values.Length;

    public bool ContainsName(string name) => _nameLookup.ContainsKey(name);

    public bool TryGetValue(string name, [NotNullWhen(true)] out EnumValue? value)
        => _nameLookup.TryGetValue(name, out value);

    internal IReadOnlyEnumValueCollection AsReadOnlyEnumValueCollection()
        => _readOnlyValues ??= new ReadOnlyEnumValueCollection(_values, _nameLookup);

    public IEnumerator<EnumValue> GetEnumerator() => Unsafe.As<IEnumerable<EnumValue>>(_values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerable<string> IReadOnlyDictionary<string, EnumValue>.Keys => _nameLookup.Keys;

    IEnumerable<EnumValue> IReadOnlyDictionary<string, EnumValue>.Values => _nameLookup.Values;

    bool IReadOnlyDictionary<string, EnumValue>.ContainsKey(string key) => _nameLookup.ContainsKey(key);

    IEnumerator<KeyValuePair<string, EnumValue>> IEnumerable<KeyValuePair<string, EnumValue>>.GetEnumerator()
        => _nameLookup.GetEnumerator();

    private sealed class ReadOnlyEnumValueCollection(
        EnumValue[] values,
        FrozenDictionary<string, EnumValue> nameLookup)
        : IReadOnlyEnumValueCollection
    {
        public IEnumValue this[string name] => nameLookup[name];

        public bool ContainsName(string name) => nameLookup.ContainsKey(name);

        public bool TryGetValue(string name, [NotNullWhen(true)] out IEnumValue? value)
        {
            if (nameLookup.TryGetValue(name, out var enumValue))
            {
                value = enumValue;
                return true;
            }

            value = null;
            return false;
        }

        public IEnumerator<IEnumValue> GetEnumerator() => new Enumerator(values);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Enumerator(EnumValue[] values) : IEnumerator<IEnumValue>
        {
            private int _index = -1;

            public IEnumValue Current => values[_index];

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++_index < values.Length;

            public void Reset() => _index = -1;

            public void Dispose() { }
        }
    }
}

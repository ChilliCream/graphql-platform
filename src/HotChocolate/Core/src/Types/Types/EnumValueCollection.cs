using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#nullable enable

namespace HotChocolate.Types;

public sealed class EnumValueCollection : IReadOnlyList<EnumValue>, IReadOnlyDictionary<string, EnumValue>
{
    private readonly EnumValue[] _values;
    private readonly FrozenDictionary<string, EnumValue> _nameLookup;
    private ReadOnlyEnumValueCollection? _readOnlyValues;

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

    private sealed class ReadOnlyEnumValueCollection : IReadOnlyEnumValueCollection
    {
        private readonly EnumValue[] _values;
        private readonly FrozenDictionary<string, EnumValue> _nameLookup;

        public ReadOnlyEnumValueCollection(EnumValue[] values, FrozenDictionary<string, EnumValue> nameLookup)
        {
            _values = values;
            _nameLookup = nameLookup;
        }

        public IEnumValue this[string name] => _nameLookup[name];

        public bool ContainsName(string name) => _nameLookup.ContainsKey(name);

        public bool TryGetValue(string name, [NotNullWhen(true)] out IEnumValue? value)
        {
            if (_nameLookup.TryGetValue(name, out var enumValue))
            {
                value = enumValue;
                return true;
            }

            value = null;
            return false;
        }

        public IEnumerator<IEnumValue> GetEnumerator() => new Enumerator(_values);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Enumerator : IEnumerator<IEnumValue>
        {
            private readonly EnumValue[] _values;
            private int _index;

            public Enumerator(EnumValue[] values)
            {
                _values = values;
                _index = -1;
            }

            public IEnumValue Current => _values[_index];

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++_index < _values.Length;

            public void Reset() => _index = -1;

            public void Dispose() { }
        }
    }
}

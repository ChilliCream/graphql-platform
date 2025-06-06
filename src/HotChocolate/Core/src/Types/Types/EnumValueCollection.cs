using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents a collection of enum values.
/// </summary>
public sealed class EnumValueCollection : IReadOnlyList<EnumValue>
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
    /// <param name="nameComparer">
    /// The name comparer that is used to compare enum names.
    /// </param>
    public EnumValueCollection(EnumValue[] values, IEqualityComparer<string> nameComparer)
    {
        _values = values;
        _nameLookup = _values.ToFrozenDictionary(v => v.Name, nameComparer);
    }

    public EnumValue this[string name] => _nameLookup[name];

    public EnumValue this[int index] => _values[index];

    public int Count => _values.Length;

    public bool ContainsName(string name) => _nameLookup.ContainsKey(name);

    public bool TryGetValue(string name, [NotNullWhen(true)] out EnumValue? value)
        => _nameLookup.TryGetValue(name, out value);

    internal IReadOnlyEnumValueCollection AsReadOnlyEnumValueCollection()
        => _readOnlyValues ??= new ReadOnlyEnumValueCollection(this);

    public IEnumerator<EnumValue> GetEnumerator() => Unsafe.As<IEnumerable<EnumValue>>(_values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class ReadOnlyEnumValueCollection(
        EnumValueCollection values)
        : IReadOnlyEnumValueCollection
    {
        public int Count => values._values.Length;

        public IEnumValue this[int index] => values._values[index];

        public IEnumValue this[string name] => values._nameLookup[name];

        public bool ContainsName(string name) => values._nameLookup.ContainsKey(name);

        public bool TryGetValue(string name, [NotNullWhen(true)] out IEnumValue? value)
        {
            if (values._nameLookup.TryGetValue(name, out var enumValue))
            {
                value = enumValue;
                return true;
            }

            value = null;
            return false;
        }

        public IEnumerator<IEnumValue> GetEnumerator()
            => values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}

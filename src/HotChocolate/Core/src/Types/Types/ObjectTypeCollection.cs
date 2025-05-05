using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#nullable enable

namespace HotChocolate.Types;

public sealed class ObjectTypeCollection
    : IReadOnlyList<ObjectType>
    , IReadOnlyDictionary<string, ObjectType>
{
    private readonly ObjectType[] _values;
    private readonly FrozenDictionary<string, ObjectType> _nameLookup;
    private ReadOnlyObjectTypeDefinitionCollection? _wrapper;

    public ObjectTypeCollection(ObjectType[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values;
        _nameLookup = _values.ToFrozenDictionary(v => v.Name, StringComparer.Ordinal);
    }

    public ObjectType this[string key] => _nameLookup[key];

    public ObjectType this[int index] => _values[index];

    public int Count => _values.Length;

    IEnumerable<string> IReadOnlyDictionary<string, ObjectType>.Keys => _nameLookup.Keys;

    IEnumerable<ObjectType> IReadOnlyDictionary<string, ObjectType>.Values => _nameLookup.Values;

    public bool ContainsName(string name)
        => _nameLookup.ContainsKey(name);

    bool IReadOnlyDictionary<string, ObjectType>.ContainsKey(string key)
        => _nameLookup.ContainsKey(key);

    public bool TryGetValue(string name, [NotNullWhen(true)] out ObjectType? value)
        => _nameLookup.TryGetValue(name, out value);

    bool IReadOnlyDictionary<string, ObjectType>.TryGetValue(string key, [NotNullWhen(true)] out ObjectType? value)
    {
        if (_nameLookup.TryGetValue(key, out var type))
        {
            value = type;
            return true;
        }

        value = null;
        return false;
    }

    internal IReadOnlyObjectTypeDefinitionCollection AsReadOnlyObjectTypeDefinitionCollection()
        => _wrapper ??= new ReadOnlyObjectTypeDefinitionCollection(_values, _nameLookup);

    public IEnumerator<ObjectType> GetEnumerator()
        => Unsafe.As<IEnumerable<ObjectType>>(_values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<KeyValuePair<string, ObjectType>> IEnumerable<KeyValuePair<string, ObjectType>>.GetEnumerator()
    {
        foreach (var (key, value) in _nameLookup)
        {
            yield return new KeyValuePair<string, ObjectType>(key, value);
        }
    }

    public static ObjectTypeCollection Empty { get; } = new ObjectTypeCollection([]);

    private sealed class ReadOnlyObjectTypeDefinitionCollection(
        ObjectType[] values,
        FrozenDictionary<string, ObjectType> nameLookup)
        : IReadOnlyObjectTypeDefinitionCollection
    {
        public bool ContainsName(string name) => nameLookup.ContainsKey(name);

        public IEnumerator<IObjectTypeDefinition> GetEnumerator() => new Enumerator(values);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Enumerator(ObjectType[] values) : IEnumerator<IObjectTypeDefinition>
        {
            private int _index = -1;

            public IObjectTypeDefinition Current => values[_index];

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++_index < values.Length;

            public void Reset() => _index = -1;

            public void Dispose() { }
        }
    }
}

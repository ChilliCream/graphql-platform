using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HotChocolate.Types;

public sealed class ObjectTypeCollection : IReadOnlyList<ObjectType>
{
    private readonly ObjectType[] _types;
    private readonly FrozenDictionary<string, ObjectType> _nameLookup;
    private ReadOnlyObjectTypeDefinitionCollection? _wrapper;

    public ObjectTypeCollection(ObjectType[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _types = values;
        _nameLookup = _types.ToFrozenDictionary(v => v.Name, StringComparer.Ordinal);
    }

    public ObjectType this[string key] => _nameLookup[key];

    public ObjectType this[int index] => _types[index];

    public int Count => _types.Length;

    public bool ContainsName(string name)
        => _nameLookup.ContainsKey(name);

    public bool TryGetValue(string name, [NotNullWhen(true)] out ObjectType? value)
        => _nameLookup.TryGetValue(name, out value);

    internal IReadOnlyObjectTypeDefinitionCollection AsReadOnlyObjectTypeDefinitionCollection()
        => _wrapper ??= new ReadOnlyObjectTypeDefinitionCollection(this);

    public IEnumerator<ObjectType> GetEnumerator()
        => Unsafe.As<IEnumerable<ObjectType>>(_types).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static ObjectTypeCollection Empty { get; } = new([]);

    private sealed class ReadOnlyObjectTypeDefinitionCollection(
        ObjectTypeCollection types)
        : IReadOnlyObjectTypeDefinitionCollection
    {
        public int Count => types._types.Length;

        public IObjectTypeDefinition this[int index] => types._types[index];

        public bool ContainsName(string name) => types._nameLookup.ContainsKey(name);

        public IEnumerator<IObjectTypeDefinition> GetEnumerator() => types.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

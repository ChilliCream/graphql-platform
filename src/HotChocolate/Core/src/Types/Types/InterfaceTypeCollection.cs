using System.Collections;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// A collection of <see cref="InterfaceType"/>s.
/// </summary>
public sealed class InterfaceTypeCollection
    : IReadOnlyList<InterfaceType>
{
    private readonly InterfaceType[] _types;
    private readonly FrozenSet<string> _typeNames;
    private ReadOnlyInterfaceTypeDefinitionCollection? _wrapper;

    public InterfaceTypeCollection(InterfaceType[] types)
    {
        ArgumentNullException.ThrowIfNull(types);
        _types = types;
        _typeNames = types.Select(t => t.Name).ToFrozenSet();
    }

    public InterfaceType this[int index]
        => _types[index];

    public int Count => _types.Length;

    public bool ContainsName(string name)
        => _typeNames.Contains(name);

    public bool Contains(InterfaceType item)
        => _types.Contains(item);

    public IEnumerator<InterfaceType> GetEnumerator()
        => Unsafe.As<IEnumerable<InterfaceType>>(_types).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    internal IReadOnlyInterfaceTypeDefinitionCollection AsReadOnlyInterfaceTypeDefinitionCollection()
        => _wrapper ??= new ReadOnlyInterfaceTypeDefinitionCollection(this);

    public static InterfaceTypeCollection Empty { get; } = new([]);

    private sealed class ReadOnlyInterfaceTypeDefinitionCollection(InterfaceTypeCollection types)
        : IReadOnlyInterfaceTypeDefinitionCollection
    {
        public IInterfaceTypeDefinition this[int index] => types._types[index];

        public int Count => types._types.Length;

        public bool ContainsName(string name)
            => types._typeNames.Contains(name);

        public IEnumerator<IInterfaceTypeDefinition> GetEnumerator()
            => types.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}

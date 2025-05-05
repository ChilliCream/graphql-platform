using System.Collections;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;

#nullable enable

namespace HotChocolate.Types;

internal sealed class InterfaceTypeCollection
    : IReadOnlyInterfaceTypeDefinitionCollection
    , IReadOnlyList<InterfaceType>
{
    private readonly InterfaceType[] _types;
    private readonly FrozenSet<string> _typeNames;

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
        => Unsafe.As<IEnumerator<InterfaceType>>(_types);

    IEnumerator<IInterfaceTypeDefinition> IEnumerable<IInterfaceTypeDefinition>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static InterfaceTypeCollection Empty { get; } = new([]);
}

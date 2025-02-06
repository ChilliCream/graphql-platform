using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class ReadOnlyInterfaceTypeDefinitionCollection
    : IInterfaceTypeDefinitionCollection
    , IReadOnlyInterfaceTypeDefinitionCollection
{
    private readonly InterfaceTypeDefinition[] _interfaces;

    private ReadOnlyInterfaceTypeDefinitionCollection(IEnumerable<InterfaceTypeDefinition> interfaces)
    {
        ArgumentNullException.ThrowIfNull(interfaces);
        _interfaces = interfaces.ToArray();
    }

    public int Count => _interfaces.Length;

    public bool IsReadOnly => true;

    public InterfaceTypeDefinition this[int index]
        => _interfaces[index];

    public void Add(InterfaceTypeDefinition definition)
        => ThrowReadOnly();

    public bool Remove(InterfaceTypeDefinition definition)
    {
        ThrowReadOnly();
        return false;
    }

    public void RemoveAt(int index) => ThrowReadOnly();

    public void Clear() => ThrowReadOnly();

    [DoesNotReturn]
    private static void ThrowReadOnly()
        => throw new NotSupportedException("Collection is read-only.");

    public bool ContainsName(string name)
        => Array.Find(_interfaces, t => t.Name.Equals(name)) is not null;

    public bool Contains(InterfaceTypeDefinition item)
        => Array.IndexOf(_interfaces, item) >= 0;

    public void CopyTo(InterfaceTypeDefinition[] array, int arrayIndex)
        => _interfaces.CopyTo(array, arrayIndex);

    public IEnumerator<InterfaceTypeDefinition> GetEnumerator()
        => ((IEnumerable<InterfaceTypeDefinition>)_interfaces).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<IReadOnlyInterfaceTypeDefinition> IEnumerable<IReadOnlyInterfaceTypeDefinition>.GetEnumerator()
        => GetEnumerator();

    public static ReadOnlyInterfaceTypeDefinitionCollection Empty { get; } = new([]);

    public static ReadOnlyInterfaceTypeDefinitionCollection From(IEnumerable<InterfaceTypeDefinition> values)
        => new(values);
}

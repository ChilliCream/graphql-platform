using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public sealed class ReadOnlyInterfaceTypeDefinitionCollection : IInterfaceTypeDefinitionCollection
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
    {
        get => _interfaces[index];
    }

    public void Add(InterfaceTypeDefinition item) => ThrowReadOnly();

    public bool Remove(InterfaceTypeDefinition item)
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
    {
        foreach (var item in _interfaces)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static ReadOnlyInterfaceTypeDefinitionCollection Empty { get; } = new([]);

    public static ReadOnlyInterfaceTypeDefinitionCollection From(IEnumerable<InterfaceTypeDefinition> values)
        => new(values);
}

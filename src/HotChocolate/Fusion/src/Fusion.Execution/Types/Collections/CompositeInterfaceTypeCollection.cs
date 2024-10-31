using System.Collections;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class CompositeInterfaceTypeCollection(CompositeInterfaceType[] interfaceTypes)
    : IReadOnlyList<CompositeInterfaceType>
{
    public int Count => interfaceTypes.Length;

    public bool IsReadOnly => false;

    public CompositeInterfaceType this[int index]
    {
        get => interfaceTypes[index];
    }

    public bool ContainsName(string name)
        => interfaceTypes.Any(t => t.Name.Equals(name, StringComparison.Ordinal));

    public bool Contains(CompositeInterfaceType item)
        => interfaceTypes.Contains(item);

    public IEnumerator<CompositeInterfaceType> GetEnumerator()
    {
        foreach (var interfaceType in interfaceTypes)
        {
            yield return interfaceType;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static CompositeInterfaceTypeCollection Empty { get; } = new([]);
}

using System.Collections;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class CompositeInterfaceTypeCollection(FusionInterfaceType[] interfaceTypes)
    : IReadOnlyList<FusionInterfaceType>
{
    public int Count => interfaceTypes.Length;

    public bool IsReadOnly => false;

    public FusionInterfaceType this[int index]
    {
        get => interfaceTypes[index];
    }

    public bool ContainsName(string name)
        => interfaceTypes.Any(t => t.Name.Equals(name, StringComparison.Ordinal));

    public bool Contains(FusionInterfaceType item)
        => interfaceTypes.Contains(item);

    public IEnumerator<FusionInterfaceType> GetEnumerator()
    {
        foreach (var interfaceType in interfaceTypes)
        {
            yield return interfaceType;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static CompositeInterfaceTypeCollection Empty { get; } = new([]);
}

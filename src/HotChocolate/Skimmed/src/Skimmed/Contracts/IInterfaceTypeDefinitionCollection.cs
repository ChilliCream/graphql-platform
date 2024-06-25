namespace HotChocolate.Skimmed;

public interface IInterfaceTypeDefinitionCollection : ICollection<InterfaceTypeDefinition>
{
    InterfaceTypeDefinition this[int index] { get; }

    bool ContainsName(string name);

    void RemoveAt(int index);
}

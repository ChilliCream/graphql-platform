namespace HotChocolate.Skimmed;

public interface IObjectTypeDefinitionCollection : ICollection<ObjectTypeDefinition>
{
    ObjectTypeDefinition this[int index] { get; }

    bool ContainsName(string name);

    void RemoveAt(int index);
}

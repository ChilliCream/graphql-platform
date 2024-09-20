namespace HotChocolate.Skimmed;

public interface IObjectTypeDefinitionCollection : ICollection<ObjectTypeDefinition>
{
    ObjectTypeDefinition this[int index] { get; }

    void RemoveAt(int index);
}

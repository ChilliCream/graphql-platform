namespace HotChocolate.Types.Relay
{
    public interface IIdSerializer
    {
        string Serialize<T>(NameString typeName, T id);

        string Serialize<T>(NameString schemaName, NameString typeName, T id);

        IdValue Deserialize(string serializedId);
    }
}

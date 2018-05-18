namespace HotChocolate.Types
{
    public interface ISerializableType
    {
        object Serialize(object value);
    }
}

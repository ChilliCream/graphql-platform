namespace HotChocolate.Types
{
    public interface ISerializableType
        : IType
    {
        object Serialize(object value);
    }
}

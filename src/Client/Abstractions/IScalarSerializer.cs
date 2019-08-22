namespace StrawberryShake
{
    public interface IScalarSerializer
    {
        string Name { get; }

        object Serialize(object value);

        object Deserialize(object serialized);
    }
}

namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles long scalars.
/// </summary>
public class LongSerializer : ScalarSerializer<long>
{
    public LongSerializer(string typeName = BuiltInScalarNames.Long)
        : base(typeName)
    {
    }
}

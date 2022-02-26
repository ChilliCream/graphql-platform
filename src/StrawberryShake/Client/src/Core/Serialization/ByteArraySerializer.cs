namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles byte array scalars.
/// </summary>
public class ByteArraySerializer : ScalarSerializer<byte[]>
{
    public ByteArraySerializer(string typeName = BuiltInScalarNames.ByteArray)
        : base(typeName)
    {
    }
}

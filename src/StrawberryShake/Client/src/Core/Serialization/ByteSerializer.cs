namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles byte scalars.
/// </summary>
public class ByteSerializer : ScalarSerializer<byte>
{
    public ByteSerializer(string typeName = BuiltInScalarNames.Byte)
        : base(typeName)
    {
    }
}
